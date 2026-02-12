using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ApiCombatGame.Data;
using ApiCombatGame.Models.DTOs.Auth;
using ApiCombatGame.Tests.Helpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ApiCombatGame.Tests.IntegrationTests;

public class DashboardFlowTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public DashboardFlowTests(WebApplicationFactory<Program> factory)
    {
        IntegrationTestSetup.DisableRateLimiting();
        var dbName = $"TestDb_Dashboard_{Guid.NewGuid()}";
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
    public async Task DashboardPage_WithoutAuth_RedirectsToLogin()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/Dashboard");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Auth/Login", response.Headers.Location?.ToString() ?? "");
    }

    [Fact]
    public async Task Profile_NewPlayer_ContainsLoginStreakFields()
    {
        var client = _factory.CreateClient();
        var (token, _) = await RegisterAndGetAuth(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/v1/player/profile");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        // Verify core profile fields that dashboard depends on
        Assert.True(root.TryGetProperty("level", out var level));
        Assert.Equal(1, level.GetInt32());

        Assert.True(root.TryGetProperty("experiencePoints", out var xp));
        Assert.Equal(0, xp.GetInt32());

        Assert.True(root.TryGetProperty("winStreak", out var streak));
        Assert.Equal(0, streak.GetInt32());

        Assert.True(root.TryGetProperty("currency", out var currency));
        Assert.Equal(1000, currency.GetInt32());
    }

    [Fact]
    public async Task Profile_NewPlayer_HasDefaultTier()
    {
        var client = _factory.CreateClient();
        var (token, _) = await RegisterAndGetAuth(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/v1/player/profile");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(content);

        Assert.Equal("Free", doc.RootElement.GetProperty("tier").GetString());
    }

    [Fact]
    public async Task Roster_NewPlayer_ReturnsStarterUnits()
    {
        var client = _factory.CreateClient();
        var (token, _) = await RegisterAndGetAuth(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/v1/player/roster");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var roster = JsonSerializer.Deserialize<List<JsonElement>>(content, JsonOptions);
        Assert.NotNull(roster);
        Assert.True(roster!.Count >= 1, "New player should have starter units");

        // Each unit should have basic properties the dashboard needs
        var unit = roster[0];
        Assert.True(unit.TryGetProperty("name", out _));
        Assert.True(unit.TryGetProperty("class", out _));
    }

    [Fact]
    public async Task Achievements_NewPlayer_NoneUnlocked()
    {
        var client = _factory.CreateClient();
        var (token, _) = await RegisterAndGetAuth(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/v1/player/achievements");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(content);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);

        foreach (var achievement in doc.RootElement.EnumerateArray())
        {
            Assert.False(achievement.GetProperty("isUnlocked").GetBoolean(),
                $"Achievement '{achievement.GetProperty("name").GetString()}' should not be unlocked for new player");
        }
    }

    [Fact]
    public async Task Challenges_NewPlayer_ReturnsOkResponse()
    {
        var client = _factory.CreateClient();
        var (token, _) = await RegisterAndGetAuth(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/v1/challenges/daily");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(content);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
    }

    [Fact]
    public async Task NotificationCount_NewPlayer_ReturnsZero()
    {
        var client = _factory.CreateClient();
        var (token, _) = await RegisterAndGetAuth(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/v1/player/notifications/count");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(content);
        Assert.Equal(0, doc.RootElement.GetProperty("unreadCount").GetInt32());
    }

    [Fact]
    public async Task Profile_ContainsGuildField_NullForNewPlayer()
    {
        var client = _factory.CreateClient();
        var (token, _) = await RegisterAndGetAuth(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/v1/player/profile");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(content);

        Assert.True(doc.RootElement.TryGetProperty("guild", out var guild));
        Assert.Equal(JsonValueKind.Null, guild.ValueKind);
    }

    [Fact]
    public async Task Profile_ContainsAchievementPoints_ZeroForNewPlayer()
    {
        var client = _factory.CreateClient();
        var (token, _) = await RegisterAndGetAuth(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/v1/player/profile");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(content);

        Assert.Equal(0, doc.RootElement.GetProperty("achievementPoints").GetInt32());
        Assert.Equal(0, doc.RootElement.GetProperty("achievementsUnlocked").GetInt32());
    }

    [Fact]
    public async Task AvailableUnits_ReturnsTemplateUnitsForShop()
    {
        var client = _factory.CreateClient();
        var (token, _) = await RegisterAndGetAuth(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/v1/player/roster/available");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(content);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
    }
}
