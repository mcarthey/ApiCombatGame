using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ApiCombatGame.Data;
using ApiCombatGame.Models.Domain;
using ApiCombatGame.Models.Enums;
using ApiCombatGame.Tests.Helpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ApiCombatGame.Tests.IntegrationTests;

public class LeaderboardApiTests : IntegrationTestBase
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public LeaderboardApiTests(WebApplicationFactory<Program> factory) : base(factory, "LeaderboardApi") { }

    [Fact]
    public async Task GetLeaderboard_ExcludesBots()
    {
        // BUG REGRESSION: Bots were appearing in leaderboard
        var (client, auth) = await CreateAuthenticatedClient();

        // Seed a bot player directly in DB
        using (var scope = Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<GameDbContext>();
            context.Players.Add(new Player
            {
                Id = Guid.NewGuid(),
                Username = "BotWarrior",
                Email = "bot@test.com",
                PasswordHash = "hashed",
                Rating = 9999,
                IsBot = true,
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
        }

        var response = await client.GetAsync("/api/v1/leaderboard");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var leaderboard = JsonSerializer.Deserialize<List<JsonElement>>(content, Json)!;

        Assert.All(leaderboard, entry =>
        {
            var username = entry.GetProperty("username").GetString();
            Assert.NotEqual("BotWarrior", username);
        });
    }

    [Fact]
    public async Task GetLeaderboard_ExcludesDeletedPlayers()
    {
        // BUG REGRESSION: Deleted players were showing on leaderboard
        var (client, _) = await CreateAuthenticatedClient();

        // Seed a deleted player
        using (var scope = Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<GameDbContext>();
            context.Players.Add(new Player
            {
                Id = Guid.NewGuid(),
                Username = "deleted-abc123",
                Email = "deleted-abc123@removed",
                PasswordHash = "",
                Rating = 9999,
                IsDeleted = true,
                DeletedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
        }

        var response = await client.GetAsync("/api/v1/leaderboard");
        var content = await response.Content.ReadAsStringAsync();
        var leaderboard = JsonSerializer.Deserialize<List<JsonElement>>(content, Json)!;

        Assert.All(leaderboard, entry =>
        {
            var username = entry.GetProperty("username").GetString()!;
            Assert.DoesNotContain("deleted-", username);
        });
    }

    [Fact]
    public async Task GetLeaderboard_OrderedByRatingDescending()
    {
        var (client, _) = await CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/v1/leaderboard");
        var content = await response.Content.ReadAsStringAsync();
        var leaderboard = JsonSerializer.Deserialize<List<JsonElement>>(content, Json)!;

        if (leaderboard.Count >= 2)
        {
            for (int i = 0; i < leaderboard.Count - 1; i++)
            {
                var current = leaderboard[i].GetProperty("rating").GetInt32();
                var next = leaderboard[i + 1].GetProperty("rating").GetInt32();
                Assert.True(current >= next, $"Rating at index {i} ({current}) should be >= index {i + 1} ({next})");
            }
        }
    }

    [Fact]
    public async Task GetLeaderboard_RespectsLimitParam()
    {
        // Register multiple players
        for (int i = 0; i < 3; i++)
            await CreateAuthenticatedClient($"lbplayer{i}_{Guid.NewGuid():N}");

        var (client, _) = await CreateAuthenticatedClient($"lbquery_{Guid.NewGuid():N}");

        var response = await client.GetAsync("/api/v1/leaderboard?limit=2");
        var content = await response.Content.ReadAsStringAsync();
        var leaderboard = JsonSerializer.Deserialize<List<JsonElement>>(content, Json)!;

        Assert.True(leaderboard.Count <= 2);
    }

    [Fact]
    public async Task GetLeaderboard_LimitClamped1To500()
    {
        var (client, _) = await CreateAuthenticatedClient();

        // Limit below 1 should be clamped to 1
        var response = await client.GetAsync("/api/v1/leaderboard?limit=0");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Limit above 500 should be clamped to 500
        response = await client.GetAsync("/api/v1/leaderboard?limit=9999");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetPlayerRanking_ReturnsCorrectRank()
    {
        var (client, auth) = await CreateAuthenticatedClient();

        var response = await client.GetAsync($"/api/v1/leaderboard/player/{auth.PlayerId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var ranking = JsonSerializer.Deserialize<JsonElement>(content, Json);
        Assert.True(ranking.GetProperty("rank").GetInt32() >= 1);
        Assert.Equal(auth.PlayerId, ranking.GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task GetPlayerRanking_BotId_Returns404()
    {
        var (client, _) = await CreateAuthenticatedClient();
        var botId = Guid.NewGuid();

        using (var scope = Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<GameDbContext>();
            context.Players.Add(new Player
            {
                Id = botId,
                Username = "LookupBot",
                Email = "lookupbot@test.com",
                PasswordHash = "hashed",
                Rating = 1500,
                IsBot = true,
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
        }

        var response = await client.GetAsync($"/api/v1/leaderboard/player/{botId}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetPlayerRanking_DeletedPlayer_Returns404()
    {
        var (client, _) = await CreateAuthenticatedClient();
        var deletedId = Guid.NewGuid();

        using (var scope = Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<GameDbContext>();
            context.Players.Add(new Player
            {
                Id = deletedId,
                Username = "deleted-xyz",
                Email = "deleted-xyz@removed",
                PasswordHash = "",
                Rating = 2000,
                IsDeleted = true,
                DeletedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
        }

        var response = await client.GetAsync($"/api/v1/leaderboard/player/{deletedId}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetLeaderboard_AnonymousAccess_Returns200()
    {
        var client = Factory.CreateClient();

        var response = await client.GetAsync("/api/v1/leaderboard");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetPlayerRanking_AnonymousAccess_Returns200()
    {
        var playerId = Guid.NewGuid();

        using (var scope = Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<GameDbContext>();
            context.Players.Add(new Player
            {
                Id = playerId,
                Username = $"anon-lookup-{Guid.NewGuid():N}",
                Email = $"anon-{Guid.NewGuid():N}@test.com",
                PasswordHash = "hashed",
                Rating = 1200,
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
        }

        var client = Factory.CreateClient();
        var response = await client.GetAsync($"/api/v1/leaderboard/player/{playerId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
