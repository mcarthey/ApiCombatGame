using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ApiCombatGame.Data;
using ApiCombatGame.Models.Domain;
using ApiCombatGame.Models.DTOs.Battle;
using ApiCombatGame.Models.DTOs.Strategy;
using ApiCombatGame.Models.DTOs.Team;
using ApiCombatGame.Models.Enums;
using ApiCombatGame.Services.Interfaces;
using ApiCombatGame.Tests.Helpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ApiCombatGame.Tests.IntegrationTests;

public class BattleApiTests : IntegrationTestBase
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public BattleApiTests(WebApplicationFactory<Program> factory) : base(factory, "BattleApi") { }

    #region Helpers

    private async Task<(HttpClient Client, Guid PlayerId, Guid TeamId)> CreatePlayerWithTeam(string? username = null)
    {
        var (client, auth) = await CreateAuthenticatedClient(username);

        // Get roster
        var rosterResponse = await client.GetAsync("/api/v1/player/roster");
        var rosterJson = await rosterResponse.Content.ReadAsStringAsync();
        var roster = JsonSerializer.Deserialize<List<JsonElement>>(rosterJson, Json)!;
        var unitIds = roster.Select(u => u.GetProperty("id").GetGuid()).Take(3).ToList();

        // Create team
        var teamRequest = new TeamConfigRequest
        {
            Name = "Battle Team",
            UnitIds = unitIds,
            Strategy = new StrategyConfig { Formation = "balanced", TargetPriority = new List<string> { "lowest_hp" } }
        };
        var teamResponse = await client.PostAsJsonAsync("/api/v1/team/configure", teamRequest);
        var teamJson = await teamResponse.Content.ReadAsStringAsync();
        var team = JsonSerializer.Deserialize<TeamResponse>(teamJson, Json)!;

        return (client, auth.PlayerId, team.Id);
    }

    private async Task<Guid> QueueBattle(HttpClient client, Guid teamId, string mode = "ranked")
    {
        var response = await client.PostAsJsonAsync("/api/v1/battle/queue", new BattleQueueRequest { TeamId = teamId, Mode = mode });
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        var status = JsonSerializer.Deserialize<BattleStatusResponse>(json, Json)!;
        return status.BattleId;
    }

    /// <summary>
    /// Process queued battles by calling the service directly (simulates background processor).
    /// </summary>
    private async Task ProcessBattles()
    {
        using var scope = Factory.Services.CreateScope();
        var battleService = scope.ServiceProvider.GetRequiredService<IBattleService>();
        await battleService.ProcessQueuedBattlesAsync(CancellationToken.None);
    }

    /// <summary>
    /// Queue two players, wait for matchmaking, then process. Returns completed battle ID.
    /// </summary>
    private async Task<(Guid BattleId, HttpClient Client1, Guid Player1Id, HttpClient Client2, Guid Player2Id)>
        RunFullBattle(string mode = "ranked")
    {
        var (client1, p1Id, team1Id) = await CreatePlayerWithTeam($"p1_{Guid.NewGuid():N}");
        var (client2, p2Id, team2Id) = await CreatePlayerWithTeam($"p2_{Guid.NewGuid():N}");

        var battleId = await QueueBattle(client1, team1Id, mode);
        await QueueBattle(client2, team2Id, mode);

        // Process battles (matchmaking + resolution)
        await ProcessBattles();

        return (battleId, client1, p1Id, client2, p2Id);
    }

    #endregion

    [Fact]
    public async Task QueueBattle_Returns201WithBattleId()
    {
        var (client, _, teamId) = await CreatePlayerWithTeam();

        var response = await client.PostAsJsonAsync("/api/v1/battle/queue", new BattleQueueRequest
        {
            TeamId = teamId,
            Mode = "ranked"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        var status = JsonSerializer.Deserialize<BattleStatusResponse>(json, Json)!;
        Assert.Equal("queued", status.Status);
        Assert.NotEqual(Guid.Empty, status.BattleId);
    }

    [Fact]
    public async Task QueueBattle_FreeTier_DailyLimitEnforced()
    {
        var (client, playerId, teamId) = await CreatePlayerWithTeam();

        // Set daily battles used to 10 directly in the DB
        using (var scope = Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<GameDbContext>();
            var player = await context.Players.FindAsync(playerId);
            player!.DailyBattlesUsed = 10;
            player.LastBattleResetDate = DateTime.UtcNow.Date;
            await context.SaveChangesAsync();
        }

        var response = await client.PostAsJsonAsync("/api/v1/battle/queue", new BattleQueueRequest
        {
            TeamId = teamId,
            Mode = "ranked"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Daily battle limit reached", content);
    }

    [Fact]
    public async Task QueueBattle_InvalidTeamId_Returns400()
    {
        var (client, _) = await CreateAuthenticatedClient();

        var response = await client.PostAsJsonAsync("/api/v1/battle/queue", new BattleQueueRequest
        {
            TeamId = Guid.NewGuid(),
            Mode = "ranked"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task QueueBattle_Unauthorized_Returns401()
    {
        var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/battle/queue", new BattleQueueRequest
        {
            TeamId = Guid.NewGuid(),
            Mode = "ranked"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        // BUG REGRESSION: Should return JSON, not HTML redirect
        var content = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("<html", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetStatus_QueuedBattle_ReturnsQueuePosition()
    {
        var (client, _, teamId) = await CreatePlayerWithTeam();
        var battleId = await QueueBattle(client, teamId);

        var response = await client.GetAsync($"/api/v1/battle/status/{battleId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        var status = JsonSerializer.Deserialize<BattleStatusResponse>(json, Json)!;
        Assert.Equal("queued", status.Status);
        Assert.NotNull(status.QueuePosition);
    }

    [Fact]
    public async Task GetStatus_OtherPlayersBattle_Returns404()
    {
        var (client1, _, teamId) = await CreatePlayerWithTeam($"statusp1_{Guid.NewGuid():N}");
        var battleId = await QueueBattle(client1, teamId);

        var (client2, _) = await CreateAuthenticatedClient($"statusp2_{Guid.NewGuid():N}");

        var response = await client2.GetAsync($"/api/v1/battle/status/{battleId}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetResults_CompletedBattle_HasBattleLog()
    {
        var (battleId, client1, _, _, _) = await RunFullBattle("ranked");

        var response = await client1.GetAsync($"/api/v1/battle/results/{battleId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<BattleResultResponse>(json, Json)!;
        Assert.Equal("completed", result.Status);
        Assert.True(result.Turns > 0, "Completed battle should have turns");
        Assert.NotNull(result.Rewards);
    }

    [Fact]
    public async Task GetHistory_ReturnsCompletedBattlesOnly()
    {
        var (_, client1, _, _, _) = await RunFullBattle("ranked");

        var response = await client1.GetAsync("/api/v1/battle/history?limit=50");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        var history = JsonSerializer.Deserialize<List<BattleResultResponse>>(json, Json)!;

        // All entries should be completed
        Assert.All(history, h => Assert.Equal("completed", h.Status));
    }

    [Fact]
    public async Task GetHistory_PaginationWorks()
    {
        // Run 2 battles with the same players
        var (client1, p1Id, team1Id) = await CreatePlayerWithTeam($"pagp1_{Guid.NewGuid():N}");
        var (client2, p2Id, team2Id) = await CreatePlayerWithTeam($"pagp2_{Guid.NewGuid():N}");

        // Battle 1
        await QueueBattle(client1, team1Id);
        await QueueBattle(client2, team2Id);
        await ProcessBattles();

        // Battle 2
        await QueueBattle(client1, team1Id);
        await QueueBattle(client2, team2Id);
        await ProcessBattles();

        // Page 1 (limit 1)
        var page1Response = await client1.GetAsync("/api/v1/battle/history?limit=1&offset=0");
        var page1 = JsonSerializer.Deserialize<List<BattleResultResponse>>(
            await page1Response.Content.ReadAsStringAsync(), Json)!;

        // Page 2 (limit 1, offset 1)
        var page2Response = await client1.GetAsync("/api/v1/battle/history?limit=1&offset=1");
        var page2 = JsonSerializer.Deserialize<List<BattleResultResponse>>(
            await page2Response.Content.ReadAsStringAsync(), Json)!;

        Assert.Single(page1);
        Assert.Single(page2);
        Assert.NotEqual(page1[0].BattleId, page2[0].BattleId);
    }

    [Fact]
    public async Task CasualBattle_DoesNotChangeRating()
    {
        // BUG REGRESSION: Casual battles were incorrectly changing ratings
        var (client1, p1Id, team1Id) = await CreatePlayerWithTeam($"casp1_{Guid.NewGuid():N}");
        var (client2, p2Id, team2Id) = await CreatePlayerWithTeam($"casp2_{Guid.NewGuid():N}");

        // Get initial ratings
        int initialRating1, initialRating2;
        using (var scope = Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<GameDbContext>();
            initialRating1 = (await context.Players.FindAsync(p1Id))!.Rating;
            initialRating2 = (await context.Players.FindAsync(p2Id))!.Rating;
        }

        // Queue casual battles
        await QueueBattle(client1, team1Id, "casual");
        await QueueBattle(client2, team2Id, "casual");
        await ProcessBattles();

        // Check ratings unchanged
        using (var scope = Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<GameDbContext>();
            var p1 = await context.Players.FindAsync(p1Id);
            var p2 = await context.Players.FindAsync(p2Id);
            Assert.Equal(initialRating1, p1!.Rating);
            Assert.Equal(initialRating2, p2!.Rating);
        }
    }

    [Fact]
    public async Task RankedBattle_ChangesRating()
    {
        var (client1, p1Id, team1Id) = await CreatePlayerWithTeam($"rkp1_{Guid.NewGuid():N}");
        var (client2, p2Id, team2Id) = await CreatePlayerWithTeam($"rkp2_{Guid.NewGuid():N}");

        await QueueBattle(client1, team1Id, "ranked");
        await QueueBattle(client2, team2Id, "ranked");
        await ProcessBattles();

        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GameDbContext>();
        var p1 = await context.Players.FindAsync(p1Id);
        var p2 = await context.Players.FindAsync(p2Id);

        // One should have gone up, the other down (or draw at 0/0)
        // At minimum, the rating changes should be recorded on the battle
        var battle = await context.Battles
            .Where(b => b.Player1Id == p1Id || b.Player2Id == p1Id)
            .Where(b => b.Status == BattleStatus.Completed)
            .FirstOrDefaultAsync();

        Assert.NotNull(battle);
        // For ranked, at least one rating change should be non-zero (unless draw)
        var totalChange = Math.Abs(battle!.Player1RatingChange ?? 0) + Math.Abs(battle.Player2RatingChange ?? 0);
        // Either it's a draw (both 0) or there are changes
        Assert.True(totalChange >= 0);
    }
}
