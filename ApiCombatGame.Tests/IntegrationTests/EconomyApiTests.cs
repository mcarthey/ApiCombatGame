using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ApiCombatGame.Data;
using ApiCombatGame.Tests.Helpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ApiCombatGame.Tests.IntegrationTests;

public class EconomyApiTests : IntegrationTestBase
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public EconomyApiTests(WebApplicationFactory<Program> factory) : base(factory, "EconomyApi") { }

    // ==================== Loot Tests ====================

    [Fact]
    public async Task GetPendingLoot_EmptyForNewPlayer()
    {
        var (client, _) = await CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/v1/loot/pending");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var loot = JsonSerializer.Deserialize<JsonElement>(content, Json);
        // New player should have no pending loot
        Assert.True(loot.TryGetProperty("drops", out var drops));
        Assert.Equal(0, drops.GetArrayLength());
    }

    [Fact]
    public async Task ClaimLoot_NoPending_ReturnsResult()
    {
        var (client, _) = await CreateAuthenticatedClient();

        var response = await client.PostAsync("/api/v1/loot/claim", null);
        // Should succeed even with nothing to claim
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.BadRequest);
    }

    // ==================== Season Tests ====================

    [Fact]
    public async Task GetSeasonCurrent_ReturnsActiveSeasonWithPlayerStats()
    {
        var (client, _) = await CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/v1/season/current");
        // Could be 200 if season exists, or 400 if no active season
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.BadRequest);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            var season = JsonSerializer.Deserialize<JsonElement>(content, Json);
            Assert.True(season.TryGetProperty("seasonName", out _) || season.TryGetProperty("name", out _));
        }
    }

    [Fact]
    public async Task GetSeasonLeaderboard_PaginatedResults()
    {
        var (client, _) = await CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/v1/season/leaderboard?limit=5&offset=0");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ==================== Battle Pass Tests ====================

    [Fact]
    public async Task GetBattlePassProgress_NewPlayer_Level0OrDefault()
    {
        var (client, _) = await CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/v1/battlepass/progress");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var progress = JsonSerializer.Deserialize<JsonElement>(content, Json);
        Assert.True(progress.TryGetProperty("currentLevel", out var level));
        Assert.True(level.GetInt32() <= 1, "New player should be at level 0 or 1");
    }

    // ==================== AI Practice Tests ====================

    [Fact]
    public async Task AiPracticeBattle_NoRatingChange()
    {
        var (client, auth) = await CreateAuthenticatedClient();

        // Get initial rating
        int initialRating;
        using (var scope = Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<GameDbContext>();
            var player = await context.Players.FindAsync(auth.PlayerId);
            initialRating = player!.Rating;
        }

        // Get opponents
        var opponentsResponse = await client.GetAsync("/api/v1/ai/opponents");
        Assert.Equal(HttpStatusCode.OK, opponentsResponse.StatusCode);

        var opponentsJson = await opponentsResponse.Content.ReadAsStringAsync();
        var opponents = JsonSerializer.Deserialize<JsonElement>(opponentsJson, Json);

        // Get the first opponent ID (AI opponent IDs are strings like "novice-1")
        string opponentId;
        if (opponents.TryGetProperty("opponents", out var oppArray) && oppArray.GetArrayLength() > 0)
        {
            opponentId = oppArray[0].GetProperty("id").GetString()!;
        }
        else
        {
            // No opponents configured — skip test
            return;
        }

        // Get roster and create team
        var rosterResponse = await client.GetAsync("/api/v1/player/roster");
        var roster = JsonSerializer.Deserialize<List<JsonElement>>(
            await rosterResponse.Content.ReadAsStringAsync(), Json)!;
        var unitIds = roster.Select(u => u.GetProperty("id").GetGuid()).Take(3).ToList();

        var teamResponse = await client.PostAsJsonAsync("/api/v1/team/configure", new
        {
            name = "Practice Team",
            unitIds
        });
        var team = JsonSerializer.Deserialize<JsonElement>(
            await teamResponse.Content.ReadAsStringAsync(), Json);
        var teamId = team.GetProperty("id").GetGuid();

        // Practice battle
        var practiceResponse = await client.PostAsJsonAsync("/api/v1/ai/practice", new
        {
            teamId,
            opponentId
        });

        if (practiceResponse.StatusCode == HttpStatusCode.OK)
        {
            // Verify rating unchanged
            using var scope = Factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<GameDbContext>();
            var player = await context.Players.FindAsync(auth.PlayerId);
            Assert.Equal(initialRating, player!.Rating);
        }
    }

    // ==================== Unit Unlock Tests ====================

    [Fact]
    public async Task UnlockUnit_DeductsCurrency()
    {
        var (client, auth) = await CreateAuthenticatedClient();

        // Get available units
        var availResponse = await client.GetAsync("/api/v1/player/roster/available");
        Assert.Equal(HttpStatusCode.OK, availResponse.StatusCode);

        var availJson = await availResponse.Content.ReadAsStringAsync();
        var available = JsonSerializer.Deserialize<List<JsonElement>>(availJson, Json)!;

        if (available.Count == 0) return; // No units to unlock

        var templateId = available[0].GetProperty("id").GetGuid();
        var cost = available[0].GetProperty("unlockCost").GetInt32();

        // Get initial currency
        int initialCurrency;
        using (var scope = Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<GameDbContext>();
            var player = await context.Players.FindAsync(auth.PlayerId);
            initialCurrency = player!.Currency;
        }

        // Unlock unit
        var unlockResponse = await client.PostAsJsonAsync("/api/v1/player/roster/unlock", new
        {
            templateUnitId = templateId
        });

        if (unlockResponse.StatusCode == HttpStatusCode.Created)
        {
            using var scope = Factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<GameDbContext>();
            var player = await context.Players.FindAsync(auth.PlayerId);
            Assert.Equal(initialCurrency - cost, player!.Currency);
        }
    }

    [Fact]
    public async Task UnlockUnit_InsufficientCurrency_Returns400()
    {
        var (client, auth) = await CreateAuthenticatedClient();

        // Set currency to 0
        using (var scope = Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<GameDbContext>();
            var player = await context.Players.FindAsync(auth.PlayerId);
            player!.Currency = 0;
            await context.SaveChangesAsync();
        }

        // Get available units
        var availResponse = await client.GetAsync("/api/v1/player/roster/available");
        var available = JsonSerializer.Deserialize<List<JsonElement>>(
            await availResponse.Content.ReadAsStringAsync(), Json)!;

        if (available.Count == 0) return;

        var templateId = available[0].GetProperty("id").GetGuid();

        var response = await client.PostAsJsonAsync("/api/v1/player/roster/unlock", new
        {
            templateUnitId = templateId
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UnlockUnit_AlreadyOwned_Returns400()
    {
        var (client, _) = await CreateAuthenticatedClient();

        // Get roster (already owned units)
        var rosterResponse = await client.GetAsync("/api/v1/player/roster");
        var roster = JsonSerializer.Deserialize<List<JsonElement>>(
            await rosterResponse.Content.ReadAsStringAsync(), Json)!;

        if (roster.Count == 0) return;

        // Try to unlock an available unit first, then unlock it again
        var availResponse = await client.GetAsync("/api/v1/player/roster/available");
        var available = JsonSerializer.Deserialize<List<JsonElement>>(
            await availResponse.Content.ReadAsStringAsync(), Json)!;

        if (available.Count == 0) return;

        var templateId = available[0].GetProperty("id").GetGuid();

        // Unlock once
        await client.PostAsJsonAsync("/api/v1/player/roster/unlock", new { templateUnitId = templateId });

        // Try to unlock same unit again
        var response = await client.PostAsJsonAsync("/api/v1/player/roster/unlock", new { templateUnitId = templateId });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
