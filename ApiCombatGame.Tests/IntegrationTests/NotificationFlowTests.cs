using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ApiCombatGame.Data;
using ApiCombatGame.Models.DTOs.Auth;
using ApiCombatGame.Services.Interfaces;
using ApiCombatGame.Tests.Helpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ApiCombatGame.Tests.IntegrationTests;

public class NotificationFlowTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public NotificationFlowTests(WebApplicationFactory<Program> factory)
    {
        IntegrationTestSetup.DisableRateLimiting();
        var dbName = $"TestDb_Notification_{Guid.NewGuid()}";
        _factory = factory.WithWebHostBuilder(builder =>
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

    private async Task<(string Token, Guid PlayerId)> RegisterAndGetAuth(HttpClient client, string? username = null)
    {
        username ??= $"test_{Guid.NewGuid():N}";
        var request = new RegisterRequest
        {
            Username = username,
            Email = $"{username}@test.com",
            Password = "SecurePass123!"
        };
        var response = await client.PostAsJsonAsync("/api/v1/auth/register", request);
        var content = await response.Content.ReadAsStringAsync();
        var auth = JsonSerializer.Deserialize<AuthResponse>(content, JsonOptions);
        return (auth!.Token, auth.PlayerId);
    }

    [Fact]
    public async Task GetNotificationCount_ReturnsZeroForNewPlayer()
    {
        var client = _factory.CreateClient();
        var (token, _) = await RegisterAndGetAuth(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/v1/player/notifications/count");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(content);
        var unreadCount = doc.RootElement.GetProperty("unreadCount").GetInt32();

        Assert.Equal(0, unreadCount);
    }

    [Fact]
    public async Task GetNotifications_ReturnsEmptyListForNewPlayer()
    {
        var client = _factory.CreateClient();
        var (token, _) = await RegisterAndGetAuth(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/v1/player/notifications?page=1&unreadOnly=false");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(content);

        var notifications = doc.RootElement.GetProperty("notifications");
        Assert.Equal(JsonValueKind.Array, notifications.ValueKind);
        Assert.Equal(0, notifications.GetArrayLength());

        var unreadCount = doc.RootElement.GetProperty("unreadCount").GetInt32();
        Assert.Equal(0, unreadCount);
    }

    [Fact]
    public async Task GetPreferences_ReturnsDefaultsForNewPlayer()
    {
        var client = _factory.CreateClient();
        var (token, _) = await RegisterAndGetAuth(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/v1/player/notifications/preferences");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(content);

        Assert.True(doc.RootElement.GetProperty("battle").GetBoolean());
        Assert.True(doc.RootElement.GetProperty("guild").GetBoolean());
        Assert.True(doc.RootElement.GetProperty("progression").GetBoolean());
        Assert.True(doc.RootElement.GetProperty("marketplace").GetBoolean());
    }

    [Fact]
    public async Task UpdatePreferences_PersistsChanges()
    {
        var client = _factory.CreateClient();
        var (token, _) = await RegisterAndGetAuth(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Update preferences with guild disabled
        var updatePayload = new NotificationPreferences
        {
            Battle = true,
            Guild = false,
            Progression = true,
            Marketplace = true
        };
        var updateResponse = await client.PutAsJsonAsync("/api/v1/player/notifications/preferences", updatePayload);
        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

        // Re-fetch preferences and verify
        var getResponse = await client.GetAsync("/api/v1/player/notifications/preferences");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var content = await getResponse.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(content);

        Assert.True(doc.RootElement.GetProperty("battle").GetBoolean());
        Assert.False(doc.RootElement.GetProperty("guild").GetBoolean());
        Assert.True(doc.RootElement.GetProperty("progression").GetBoolean());
        Assert.True(doc.RootElement.GetProperty("marketplace").GetBoolean());
    }

    [Fact]
    public async Task MarkAllRead_ReturnsNoContent()
    {
        var client = _factory.CreateClient();
        var (token, _) = await RegisterAndGetAuth(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsync("/api/v1/player/notifications/read-all", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task PreferencesRoundTrip_DisableBattle_OthersRemainTrue()
    {
        var client = _factory.CreateClient();
        var (token, _) = await RegisterAndGetAuth(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Verify defaults first
        var initialResponse = await client.GetAsync("/api/v1/player/notifications/preferences");
        Assert.Equal(HttpStatusCode.OK, initialResponse.StatusCode);
        var initialContent = await initialResponse.Content.ReadAsStringAsync();
        var initialDoc = JsonDocument.Parse(initialContent);
        Assert.True(initialDoc.RootElement.GetProperty("battle").GetBoolean());

        // Disable battle, keep the rest at defaults
        var updatePayload = new NotificationPreferences
        {
            Battle = false,
            Guild = true,
            Progression = true,
            Marketplace = true
        };
        var updateResponse = await client.PutAsJsonAsync("/api/v1/player/notifications/preferences", updatePayload);
        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

        // Re-fetch and verify battle is false, others remain true
        var verifyResponse = await client.GetAsync("/api/v1/player/notifications/preferences");
        Assert.Equal(HttpStatusCode.OK, verifyResponse.StatusCode);

        var verifyContent = await verifyResponse.Content.ReadAsStringAsync();
        var verifyDoc = JsonDocument.Parse(verifyContent);

        Assert.False(verifyDoc.RootElement.GetProperty("battle").GetBoolean());
        Assert.True(verifyDoc.RootElement.GetProperty("guild").GetBoolean());
        Assert.True(verifyDoc.RootElement.GetProperty("progression").GetBoolean());
        Assert.True(verifyDoc.RootElement.GetProperty("marketplace").GetBoolean());
    }

    [Fact]
    public async Task GetNotificationCount_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/player/notifications/count");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetNotifications_WithPagination_ReturnsPageInfo()
    {
        var client = _factory.CreateClient();
        var (token, _) = await RegisterAndGetAuth(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/v1/player/notifications?page=2&unreadOnly=true");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(content);

        // Should return page number matching the request
        Assert.Equal(2, doc.RootElement.GetProperty("page").GetInt32());
        Assert.Equal(JsonValueKind.Array, doc.RootElement.GetProperty("notifications").ValueKind);
    }
}
