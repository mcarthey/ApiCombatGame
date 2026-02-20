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

public class PlayerProgressionFlowTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public PlayerProgressionFlowTests(WebApplicationFactory<Program> factory)
    {
        IntegrationTestSetup.DisableRateLimiting();
        var dbName = $"TestDb_Progression_{Guid.NewGuid()}";
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<GameDbContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                services.AddDbContext<GameDbContext>(options =>
                    options.UseInMemoryDatabase(dbName)
                        .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning)));
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
    public async Task GetProfile_ReturnsNewPlayerDefaults()
    {
        var client = _factory.CreateClient();
        var (token, playerId) = await RegisterAndGetAuth(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/v1/player/profile");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        Assert.Equal(1, root.GetProperty("level").GetInt32());
        Assert.Equal(0, root.GetProperty("experiencePoints").GetInt32());
        Assert.Equal(1000, root.GetProperty("currency").GetInt32());
        Assert.Equal(1000, root.GetProperty("rating").GetInt32());
        Assert.Equal(0, root.GetProperty("winStreak").GetInt32());
        Assert.Equal("Free", root.GetProperty("tier").GetString());
    }

    [Fact]
    public async Task GetProfile_ContainsProgressionFields()
    {
        var client = _factory.CreateClient();
        var (token, _) = await RegisterAndGetAuth(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/v1/player/profile");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        // Verify progression-related fields exist and have expected types
        Assert.True(root.TryGetProperty("experiencePoints", out var xp));
        Assert.Equal(JsonValueKind.Number, xp.ValueKind);

        Assert.True(root.TryGetProperty("xpToNextLevel", out var xpNext));
        Assert.Equal(JsonValueKind.Number, xpNext.ValueKind);

        Assert.True(root.TryGetProperty("winStreak", out var streak));
        Assert.Equal(JsonValueKind.Number, streak.ValueKind);

        Assert.True(root.TryGetProperty("level", out var level));
        Assert.Equal(JsonValueKind.Number, level.ValueKind);

        Assert.True(root.TryGetProperty("rating", out var rating));
        Assert.Equal(JsonValueKind.Number, rating.ValueKind);
    }

    [Fact]
    public async Task GetProfile_ContainsGuildField()
    {
        var client = _factory.CreateClient();
        var (token, _) = await RegisterAndGetAuth(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/v1/player/profile");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        // Guild field should exist and be null for a new player
        Assert.True(root.TryGetProperty("guild", out var guild));
        Assert.Equal(JsonValueKind.Null, guild.ValueKind);
    }

    [Fact]
    public async Task GetProfile_ContainsAchievementFields()
    {
        var client = _factory.CreateClient();
        var (token, _) = await RegisterAndGetAuth(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/v1/player/profile");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("achievementPoints", out var points));
        Assert.Equal(JsonValueKind.Number, points.ValueKind);

        Assert.True(root.TryGetProperty("achievementsUnlocked", out var unlocked));
        Assert.Equal(JsonValueKind.Number, unlocked.ValueKind);

        // New player should have 0 achievement points and 0 unlocked
        Assert.Equal(0, points.GetInt32());
        Assert.Equal(0, unlocked.GetInt32());
    }

    [Fact]
    public async Task GetAchievements_ReturnsListWithNoneUnlockedForNewPlayer()
    {
        var client = _factory.CreateClient();
        var (token, _) = await RegisterAndGetAuth(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/v1/player/achievements");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(content);

        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);

        // The endpoint returns all achievement definitions with player progress.
        // For a new player, none should be unlocked.
        foreach (var achievement in doc.RootElement.EnumerateArray())
        {
            Assert.False(achievement.GetProperty("isUnlocked").GetBoolean(),
                $"Achievement '{achievement.GetProperty("name").GetString()}' should not be unlocked for a new player");
            Assert.Equal(0, achievement.GetProperty("progress").GetInt32());
        }
    }

    [Fact]
    public async Task GetProfile_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/player/profile");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetProfile_ContainsAllExpectedFields()
    {
        var client = _factory.CreateClient();
        var (token, playerId) = await RegisterAndGetAuth(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/v1/player/profile");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        // Verify all documented profile fields are present
        Assert.True(root.TryGetProperty("id", out _));
        Assert.True(root.TryGetProperty("username", out _));
        Assert.True(root.TryGetProperty("email", out _));
        Assert.True(root.TryGetProperty("level", out _));
        Assert.True(root.TryGetProperty("experiencePoints", out _));
        Assert.True(root.TryGetProperty("xpToNextLevel", out _));
        Assert.True(root.TryGetProperty("currency", out _));
        Assert.True(root.TryGetProperty("rating", out _));
        Assert.True(root.TryGetProperty("winStreak", out _));
        Assert.True(root.TryGetProperty("tier", out _));
        Assert.True(root.TryGetProperty("achievementPoints", out _));
        Assert.True(root.TryGetProperty("achievementsUnlocked", out _));
        Assert.True(root.TryGetProperty("guild", out _));
        Assert.True(root.TryGetProperty("createdAt", out _));
        Assert.True(root.TryGetProperty("lastLoginAt", out _));
        Assert.True(root.TryGetProperty("rosterCount", out _));
        Assert.True(root.TryGetProperty("teamCount", out _));

        // Verify player ID matches what was returned during registration
        Assert.Equal(playerId, root.GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task GetAchievements_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/player/achievements");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
