using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ApiCombatGame.Data;
using ApiCombatGame.Models.DTOs.Auth;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ApiCombatGame.Tests.Helpers;

/// <summary>
/// Shared base class for integration tests. Provides factory setup, in-memory DB,
/// and auth helper methods to reduce boilerplate.
/// </summary>
public abstract class IntegrationTestBase : IClassFixture<WebApplicationFactory<Program>>
{
    protected readonly WebApplicationFactory<Program> Factory;
    protected static readonly JsonSerializerOptions JsonOptions =
        new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    protected IntegrationTestBase(WebApplicationFactory<Program> factory, string dbPrefix)
    {
        IntegrationTestSetup.DisableRateLimiting();
        var dbName = $"TestDb_{dbPrefix}_{Guid.NewGuid()}";
        Factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<GameDbContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                services.AddDbContext<GameDbContext>(options =>
                    options.UseInMemoryDatabase(dbName));
            });
        });
    }

    /// <summary>Creates a new HttpClient with default settings.</summary>
    protected HttpClient CreateClient() => Factory.CreateClient();

    /// <summary>Creates a new HttpClient that does not follow redirects.</summary>
    protected HttpClient CreateClientNoRedirect() => Factory.CreateClient(
        new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    /// <summary>Registers a new player and returns the auth response with token and player ID.</summary>
    protected async Task<AuthResponse> RegisterPlayer(HttpClient client, string? username = null)
    {
        username ??= $"test_{Guid.NewGuid():N}";
        var request = new RegisterRequest
        {
            Username = username,
            Email = $"{username}@test.com",
            Password = "SecurePass123!"
        };
        var response = await client.PostAsJsonAsync("/api/v1/auth/register", request);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<AuthResponse>(content, JsonOptions)!;
    }

    /// <summary>Creates a new HttpClient, registers a player, and sets the auth header.</summary>
    protected async Task<(HttpClient Client, AuthResponse Auth)> CreateAuthenticatedClient(string? username = null)
    {
        var client = CreateClient();
        var auth = await RegisterPlayer(client, username);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
        return (client, auth);
    }

    /// <summary>Creates an authenticated client with educator privileges (IsEducator + EmailConfirmed).</summary>
    protected async Task<(HttpClient Client, AuthResponse Auth)> CreateEducatorClient(string? username = null)
    {
        var (client, auth) = await CreateAuthenticatedClient(username);
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GameDbContext>();
        var player = await context.Players.FindAsync(auth.PlayerId);
        player!.IsEducator = true;
        player.EmailConfirmed = true;
        await context.SaveChangesAsync();
        return (client, auth);
    }
}
