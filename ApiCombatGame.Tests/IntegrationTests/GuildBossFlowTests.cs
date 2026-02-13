using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ApiCombatGame.Data;
using ApiCombatGame.Models.DTOs.Guild;
using ApiCombatGame.Models.Enums;
using ApiCombatGame.Tests.Helpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ApiCombatGame.Tests.IntegrationTests;

public class GuildBossFlowTests : IntegrationTestBase
{
    public GuildBossFlowTests(WebApplicationFactory<Program> factory)
        : base(factory, "GuildBoss")
    {
    }

    [Fact]
    public async Task GetCurrentBoss_NotInGuild_ReturnsNotFound()
    {
        var (client, _) = await CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/v1/guild/boss/current");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateGuild_FreePlayer_ReturnsForbidden()
    {
        var (client, _) = await CreateAuthenticatedClient();

        var response = await client.PostAsJsonAsync("/api/v1/guild/create", new
        {
            name = "Test Guild",
            tag = "TST",
            description = "A test guild"
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetCurrentBoss_InGuildNoBoss_ReturnsNotFound()
    {
        // Register and upgrade to Premium tier so guild creation works
        var (client, auth) = await CreateAuthenticatedClient();
        await UpgradePlayerTier(auth.PlayerId, SubscriptionTier.Premium);

        var createGuildResponse = await client.PostAsJsonAsync("/api/v1/guild/create", new
        {
            name = "Boss Test Guild",
            tag = "BTG",
            description = "A test guild for boss tests"
        });
        Assert.Equal(HttpStatusCode.Created, createGuildResponse.StatusCode);

        var response = await client.GetAsync("/api/v1/guild/boss/current");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AttemptBoss_NotInGuild_ReturnsNotFoundOrBadRequest()
    {
        var (client, _) = await CreateAuthenticatedClient();

        var response = await client.PostAsJsonAsync("/api/v1/guild/boss/attempt", new
        {
            bossId = Guid.NewGuid(),
            teamId = Guid.NewGuid()
        });

        Assert.True(
            response.StatusCode is HttpStatusCode.NotFound
                or HttpStatusCode.BadRequest
                or HttpStatusCode.NotImplemented,
            $"Expected 404, 400, or 501 but got {response.StatusCode}");
    }

    [Fact]
    public async Task GetBossLeaderboard_EmptyBoss_ReturnsOk()
    {
        var (client, _) = await CreateAuthenticatedClient();
        var bossId = Guid.NewGuid();

        var response = await client.GetAsync($"/api/v1/guild/boss/leaderboard?bossId={bossId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var items = JsonSerializer.Deserialize<JsonElement>(content, JsonOptions);
        Assert.Equal(JsonValueKind.Array, items.ValueKind);
        Assert.Equal(0, items.GetArrayLength());
    }

    [Fact]
    public async Task GuildBossFlow_CreateGuildAndCheckBoss()
    {
        // Register and upgrade to Premium
        var (client, auth) = await CreateAuthenticatedClient();
        await UpgradePlayerTier(auth.PlayerId, SubscriptionTier.Premium);

        // Create a guild
        var createGuildResponse = await client.PostAsJsonAsync("/api/v1/guild/create", new
        {
            name = $"Boss Flow Guild {Guid.NewGuid():N}",
            tag = "BFG",
            description = "Guild for boss flow integration test"
        });
        Assert.Equal(HttpStatusCode.Created, createGuildResponse.StatusCode);

        // No boss should be spawned yet
        var bossResponse = await client.GetAsync("/api/v1/guild/boss/current");
        Assert.Equal(HttpStatusCode.NotFound, bossResponse.StatusCode);

        // Leaderboard should return OK with empty results
        var leaderboardResponse = await client.GetAsync($"/api/v1/guild/boss/leaderboard?bossId={Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.OK, leaderboardResponse.StatusCode);

        var leaderboardContent = await leaderboardResponse.Content.ReadAsStringAsync();
        var leaderboard = JsonSerializer.Deserialize<JsonElement>(leaderboardContent, JsonOptions);
        Assert.Equal(JsonValueKind.Array, leaderboard.ValueKind);
    }

    /// <summary>Directly updates the player's tier in the database for testing tier-gated features.</summary>
    private async Task UpgradePlayerTier(Guid playerId, SubscriptionTier tier)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GameDbContext>();
        var player = await db.Players.FindAsync(playerId);
        Assert.NotNull(player);
        player!.CurrentTier = tier;
        await db.SaveChangesAsync();
    }
}
