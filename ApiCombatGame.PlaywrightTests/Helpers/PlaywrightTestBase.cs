using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using ApiCombatGame.Data;
using ApiCombatGame.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Xunit;

namespace ApiCombatGame.PlaywrightTests.Helpers;

/// <summary>
/// Custom WebApplicationFactory that exposes the app via a real TCP port for Playwright.
///
/// WebApplicationFactory always adds UseTestServer() and casts IServer to TestServer —
/// there's no clean way to replace it with Kestrel. Instead, we let TestServer be the
/// IServer (so the cast succeeds), then start a lightweight Kestrel proxy that forwards
/// all HTTP requests through TestServer's HttpMessageHandler.
/// </summary>
internal class PlaywrightWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _url;
    private readonly string _dbName;
    private WebApplication? _proxyApp;
    private HttpMessageInvoker? _invoker;

    public PlaywrightWebApplicationFactory(string url, string dbName)
    {
        _url = url;
        _dbName = dbName;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Swap to in-memory DB
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<GameDbContext>));
            if (descriptor != null) services.Remove(descriptor);

            services.AddDbContext<GameDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Let base build with TestServer (WAF requires IServer = TestServer for its cast)
        var testHost = base.CreateHost(builder);

        // Get TestServer's handler — this sends requests through the full ASP.NET Core pipeline
        var testServer = (TestServer)testHost.Services.GetRequiredService<IServer>();
        _invoker = new HttpMessageInvoker(testServer.CreateHandler(), disposeHandler: false);

        // Start a lightweight Kestrel proxy that exposes TestServer via real TCP
        _proxyApp = CreateProxy();
        _proxyApp.Start();

        return testHost;
    }

    private WebApplication CreateProxy()
    {
        var proxyBuilder = WebApplication.CreateBuilder();
        proxyBuilder.WebHost.UseUrls(_url);
        proxyBuilder.Logging.ClearProviders();

        var app = proxyBuilder.Build();
        var invoker = _invoker!;

        app.Run(async context =>
        {
            var targetUri = new Uri($"http://localhost{context.Request.Path}{context.Request.QueryString}");

            var proxyReq = new HttpRequestMessage(
                new HttpMethod(context.Request.Method), targetUri);

            // Forward request headers (skip Host, set it explicitly so auth
            // redirects point back to the proxy, not to "localhost")
            foreach (var header in context.Request.Headers)
            {
                if (string.Equals(header.Key, "Host", StringComparison.OrdinalIgnoreCase))
                    continue;
                proxyReq.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            }
            proxyReq.Headers.Host = context.Request.Host.ToString();

            // Forward request body
            if (context.Request.ContentLength > 0 || context.Request.ContentType != null)
            {
                proxyReq.Content = new StreamContent(context.Request.Body);
                if (context.Request.ContentType != null)
                    proxyReq.Content.Headers.ContentType =
                        MediaTypeHeaderValue.Parse(context.Request.ContentType);
                if (context.Request.ContentLength.HasValue)
                    proxyReq.Content.Headers.ContentLength = context.Request.ContentLength;
            }

            // Send through TestServer pipeline
            var resp = await invoker.SendAsync(proxyReq, context.RequestAborted);

            // Copy response status + headers
            context.Response.StatusCode = (int)resp.StatusCode;
            foreach (var header in resp.Headers)
                context.Response.Headers[header.Key] = header.Value.ToArray();
            foreach (var header in resp.Content.Headers)
                context.Response.Headers[header.Key] = header.Value.ToArray();
            context.Response.Headers.Remove("transfer-encoding");

            // Copy response body
            await resp.Content.CopyToAsync(context.Response.Body);
        });

        return app;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _proxyApp?.StopAsync().GetAwaiter().GetResult();
            _proxyApp?.DisposeAsync().AsTask().GetAwaiter().GetResult();
            _invoker?.Dispose();
        }
        base.Dispose(disposing);
    }
}

/// <summary>
/// Base class for Playwright tests. Starts the ASP.NET Core app on a real Kestrel
/// TCP port with an in-memory database, then connects Playwright Chromium.
/// </summary>
public abstract class PlaywrightTestBase : IAsyncLifetime
{
    private PlaywrightWebApplicationFactory? _factory;
    protected IPlaywright PlaywrightInstance = null!;
    protected IBrowser Browser = null!;
    protected string BaseUrl = null!;

    public async Task InitializeAsync()
    {
        RateLimitingMiddleware.Enabled = false;

        var port = GetFreePort();
        BaseUrl = $"http://127.0.0.1:{port}";
        var dbName = $"Playwright_{Guid.NewGuid()}";

        _factory = new PlaywrightWebApplicationFactory(BaseUrl, dbName);

        // Trigger host creation → TestServer starts, Kestrel proxy starts on BaseUrl
        _ = _factory.Services;

        // Initialize Playwright
        PlaywrightInstance = await Playwright.CreateAsync();
        Browser = await PlaywrightInstance.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });
    }

    public async Task DisposeAsync()
    {
        if (Browser != null)
            await Browser.DisposeAsync();

        PlaywrightInstance?.Dispose();

        if (_factory != null)
            await _factory.DisposeAsync();
    }

    protected async Task<IPage> NewPageAsync()
    {
        var context = await Browser.NewContextAsync();
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(15000);
        return page;
    }

    private static int GetFreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
