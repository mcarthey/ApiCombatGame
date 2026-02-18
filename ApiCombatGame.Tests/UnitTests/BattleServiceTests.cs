using System.Text.Json;
using ApiCombatGame.Data;
using ApiCombatGame.Models.Domain;
using ApiCombatGame.Models.DTOs.Battle;
using ApiCombatGame.Models.DTOs.Progression;
using ApiCombatGame.Models.DTOs.Strategy;
using ApiCombatGame.Models.Enums;
using ApiCombatGame.Services;
using ApiCombatGame.Services.Interfaces;
using ApiCombatGame.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ApiCombatGame.Tests.UnitTests;

public class BattleServiceTests : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly GameDbContext _context;
    private readonly Mock<IStrategyEngine> _strategyEngine;
    private readonly Mock<IMatchmakingService> _matchmaking;
    private readonly Mock<IPlayerProgressionService> _progression;
    private readonly Mock<IAchievementService> _achievements;
    private readonly Mock<ISeasonService> _season;
    private readonly Mock<ILootService> _loot;
    private readonly Mock<IRivalService> _rival;
    private readonly Mock<IBattlePassService> _battlePass;
    private readonly Mock<IGuildWarService> _guildWar;
    private readonly Mock<IActivityFeedService> _activityFeed;
    private readonly Mock<INotificationService> _notifications;
    private readonly Mock<ILogger<BattleService>> _logger;
    private readonly IConfiguration _config;
    private readonly BattleService _service;

    public BattleServiceTests()
    {
        _context = TestDbContextFactory.Create();
        _strategyEngine = new Mock<IStrategyEngine>();
        _matchmaking = new Mock<IMatchmakingService>();
        _progression = new Mock<IPlayerProgressionService>();
        _achievements = new Mock<IAchievementService>();
        _season = new Mock<ISeasonService>();
        _loot = new Mock<ILootService>();
        _rival = new Mock<IRivalService>();
        _battlePass = new Mock<IBattlePassService>();
        _guildWar = new Mock<IGuildWarService>();
        _activityFeed = new Mock<IActivityFeedService>();
        _notifications = new Mock<INotificationService>();
        _logger = new Mock<ILogger<BattleService>>();

        _config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["GameSettings:MaxTurnsPerBattle"] = "50"
            })
            .Build();

        // Default mock: progression returns a basic reward summary
        _progression.Setup(p => p.ProcessBattleRewardsAsync(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<int>()))
            .ReturnsAsync(new BattleRewardsSummary { GoldEarned = 50, XpEarned = 25, TierMultiplier = 1.0m });

        _service = new BattleService(
            _context,
            _strategyEngine.Object,
            _matchmaking.Object,
            _progression.Object,
            _achievements.Object,
            _season.Object,
            _loot.Object,
            _rival.Object,
            _battlePass.Object,
            _guildWar.Object,
            _activityFeed.Object,
            new ActivityLedger(_context),
            _notifications.Object,
            _config,
            _logger.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region Helpers

    private Player CreatePlayer(string username = "testplayer", int rating = 1000, SubscriptionTier tier = SubscriptionTier.Free)
    {
        var player = new Player
        {
            Id = Guid.NewGuid(),
            Username = username,
            Email = $"{username}@test.com",
            PasswordHash = "hashed",
            Rating = rating,
            CurrentTier = tier,
            Currency = 1000,
            Level = 1,
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow,
            LastBattleResetDate = DateTime.UtcNow.Date
        };
        _context.Players.Add(player);
        _context.SaveChanges();
        return player;
    }

    private (Team team, List<Unit> units) CreateTeamWithUnits(Guid playerId, int unitCount = 3)
    {
        var units = new List<Unit>();
        for (int i = 0; i < unitCount; i++)
        {
            var unit = new Unit
            {
                Id = Guid.NewGuid(),
                Name = $"Unit{i}",
                Class = UnitClass.Warrior,
                Health = 100,
                Attack = 20,
                Defense = 10,
                Speed = 15,
                PlayerId = playerId,
                Level = 1,
                Abilities = new List<Ability>
                {
                    new Ability
                    {
                        Id = Guid.NewGuid(),
                        Name = "Basic Attack",
                        Type = AbilityType.BasicAttack,
                        Damage = 15,
                        CooldownTurns = 0,
                        Description = "A basic attack"
                    }
                }
            };
            units.Add(unit);
        }

        _context.Units.AddRange(units);
        _context.SaveChanges();

        var team = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Test Team",
            PlayerId = playerId,
            UnitIdsJson = JsonSerializer.Serialize(units.Select(u => u.Id).ToList(), JsonOptions),
            StrategyJson = "{}"
        };
        _context.Teams.Add(team);
        _context.SaveChanges();

        return (team, units);
    }

    private void SetupStrategyEngineForWinner(int winnerTeam)
    {
        _strategyEngine.Setup(s => s.ResolveBattle(
                It.IsAny<List<Unit>>(), It.IsAny<StrategyConfig>(),
                It.IsAny<List<Unit>>(), It.IsAny<StrategyConfig>(),
                It.IsAny<int>(), It.IsAny<int?>()))
            .Returns(new BattleResolution
            {
                WinnerTeam = winnerTeam,
                TotalTurns = 10,
                Log = new List<BattleLogEntry>
                {
                    new() { Turn = 1, Actor = "Unit0", Action = "Basic Attack", Target = "Unit1", Damage = 15, TargetHpRemaining = 85 }
                }
            });
    }

    private async Task<(Battle battle1, Battle battle2, Player p1, Player p2)> SetupMatchedBattle(
        string mode = "ranked", int p1Rating = 1000, int p2Rating = 1000)
    {
        var p1 = CreatePlayer("player1", p1Rating);
        var p2 = CreatePlayer("player2", p2Rating);
        var (team1, _) = CreateTeamWithUnits(p1.Id);
        var (team2, _) = CreateTeamWithUnits(p2.Id);

        var battle1 = new Battle
        {
            Id = Guid.NewGuid(),
            Player1Id = p1.Id,
            Team1Id = team1.Id,
            Status = BattleStatus.Queued,
            Mode = mode,
            QueuedAt = DateTime.UtcNow
        };

        var battle2 = new Battle
        {
            Id = Guid.NewGuid(),
            Player1Id = p2.Id,
            Team1Id = team2.Id,
            Status = BattleStatus.Queued,
            Mode = mode,
            QueuedAt = DateTime.UtcNow
        };

        _context.Battles.AddRange(battle1, battle2);
        await _context.SaveChangesAsync();

        _matchmaking.Setup(m => m.FindMatchAsync())
            .ReturnsAsync((battle1, battle2));

        return (battle1, battle2, p1, p2);
    }

    #endregion

    // ==================== QueueBattle Tests ====================

    [Fact]
    public async Task QueueBattle_ValidTeam_CreatesQueuedBattle()
    {
        var player = CreatePlayer();
        var (team, _) = CreateTeamWithUnits(player.Id);

        var result = await _service.QueueBattleAsync(player.Id, new BattleQueueRequest
        {
            TeamId = team.Id,
            Mode = "ranked"
        });

        Assert.Equal("queued", result.Status);
        Assert.True(result.QueuePosition > 0);

        var battle = await _context.Battles.FirstOrDefaultAsync(b => b.Id == result.BattleId);
        Assert.NotNull(battle);
        Assert.Equal(BattleStatus.Queued, battle.Status);
        Assert.Equal(player.Id, battle.Player1Id);
    }

    [Fact]
    public async Task QueueBattle_EmptyTeam_Throws()
    {
        var player = CreatePlayer();
        var emptyTeam = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Empty Team",
            PlayerId = player.Id,
            UnitIdsJson = "[]"
        };
        _context.Teams.Add(emptyTeam);
        await _context.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.QueueBattleAsync(player.Id, new BattleQueueRequest { TeamId = emptyTeam.Id }));
    }

    [Fact]
    public async Task QueueBattle_AlreadyQueued_Throws()
    {
        var player = CreatePlayer();
        var (team, _) = CreateTeamWithUnits(player.Id);

        // Queue first battle
        await _service.QueueBattleAsync(player.Id, new BattleQueueRequest { TeamId = team.Id });

        // Try to queue again
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.QueueBattleAsync(player.Id, new BattleQueueRequest { TeamId = team.Id }));
    }

    [Fact]
    public async Task QueueBattle_OtherPlayersTeam_Throws()
    {
        var player1 = CreatePlayer("player1");
        var player2 = CreatePlayer("player2");
        var (team2, _) = CreateTeamWithUnits(player2.Id);

        // Player1 tries to use player2's team
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.QueueBattleAsync(player1.Id, new BattleQueueRequest { TeamId = team2.Id }));
    }

    // ==================== ProcessBattle Tests ====================

    [Fact]
    public async Task ProcessBattle_RankedMode_UpdatesRatings()
    {
        var (_, _, p1, p2) = await SetupMatchedBattle("ranked", 1000, 1000);
        SetupStrategyEngineForWinner(1); // P1 wins

        await _service.ProcessQueuedBattlesAsync(CancellationToken.None);

        var updatedP1 = await _context.Players.FindAsync(p1.Id);
        var updatedP2 = await _context.Players.FindAsync(p2.Id);

        Assert.True(updatedP1!.Rating > 1000, "Winner rating should increase");
        Assert.True(updatedP2!.Rating < 1000, "Loser rating should decrease");
    }

    [Fact]
    public async Task ProcessBattle_CasualMode_NoRatingChange()
    {
        // BUG REGRESSION: Casual battles were incorrectly applying ELO rating changes
        var (_, _, p1, p2) = await SetupMatchedBattle("casual", 1000, 1000);
        SetupStrategyEngineForWinner(1); // P1 wins

        await _service.ProcessQueuedBattlesAsync(CancellationToken.None);

        var updatedP1 = await _context.Players.FindAsync(p1.Id);
        var updatedP2 = await _context.Players.FindAsync(p2.Id);

        Assert.Equal(1000, updatedP1!.Rating);
        Assert.Equal(1000, updatedP2!.Rating);
    }

    [Fact]
    public async Task ProcessBattle_CasualMode_StillAwardsGoldXP()
    {
        var (_, _, p1, p2) = await SetupMatchedBattle("casual", 1000, 1000);
        SetupStrategyEngineForWinner(1);

        await _service.ProcessQueuedBattlesAsync(CancellationToken.None);

        // Progression service should still be called for both players
        _progression.Verify(p => p.ProcessBattleRewardsAsync(p1.Id, true, 0), Times.Once);
        _progression.Verify(p => p.ProcessBattleRewardsAsync(p2.Id, false, 0), Times.Once);
    }

    [Fact]
    public async Task ProcessBattle_UnitsWithoutAbilities_CancelsGracefully()
    {
        // BUG REGRESSION: Units without abilities caused strategy engine to throw,
        // which was silently caught and turned into a cancellation
        var p1 = CreatePlayer("player1");
        var p2 = CreatePlayer("player2");

        // Create units WITHOUT abilities
        var unit1 = new Unit
        {
            Id = Guid.NewGuid(), Name = "NoAbilityUnit", Class = UnitClass.Warrior,
            Health = 100, Attack = 20, Defense = 10, Speed = 15, PlayerId = p1.Id
        };
        var unit2 = new Unit
        {
            Id = Guid.NewGuid(), Name = "NoAbilityUnit2", Class = UnitClass.Warrior,
            Health = 100, Attack = 20, Defense = 10, Speed = 15, PlayerId = p2.Id
        };
        _context.Units.AddRange(unit1, unit2);
        await _context.SaveChangesAsync();

        var team1 = new Team
        {
            Id = Guid.NewGuid(), Name = "Team1", PlayerId = p1.Id,
            UnitIdsJson = JsonSerializer.Serialize(new List<Guid> { unit1.Id }, JsonOptions)
        };
        var team2 = new Team
        {
            Id = Guid.NewGuid(), Name = "Team2", PlayerId = p2.Id,
            UnitIdsJson = JsonSerializer.Serialize(new List<Guid> { unit2.Id }, JsonOptions)
        };
        _context.Teams.AddRange(team1, team2);
        await _context.SaveChangesAsync();

        var battle1 = new Battle
        {
            Id = Guid.NewGuid(), Player1Id = p1.Id, Team1Id = team1.Id,
            Status = BattleStatus.Queued, Mode = "ranked", QueuedAt = DateTime.UtcNow
        };
        var battle2 = new Battle
        {
            Id = Guid.NewGuid(), Player1Id = p2.Id, Team1Id = team2.Id,
            Status = BattleStatus.Queued, Mode = "ranked", QueuedAt = DateTime.UtcNow
        };
        _context.Battles.AddRange(battle1, battle2);
        await _context.SaveChangesAsync();

        _matchmaking.Setup(m => m.FindMatchAsync()).ReturnsAsync((battle1, battle2));

        await _service.ProcessQueuedBattlesAsync(CancellationToken.None);

        var updated = await _context.Battles.FindAsync(battle1.Id);
        Assert.Equal(BattleStatus.Cancelled, updated!.Status);

        // Strategy engine should NOT have been called
        _strategyEngine.Verify(s => s.ResolveBattle(
            It.IsAny<List<Unit>>(), It.IsAny<StrategyConfig>(),
            It.IsAny<List<Unit>>(), It.IsAny<StrategyConfig>(),
            It.IsAny<int>(), It.IsAny<int?>()), Times.Never);
    }

    [Fact]
    public async Task ProcessBattle_TeamNull_CancelsBattle()
    {
        var p1 = CreatePlayer("player1");
        var p2 = CreatePlayer("player2");
        var (team1, _) = CreateTeamWithUnits(p1.Id);

        // Note: team2 ID doesn't exist
        var fakeTeamId = Guid.NewGuid();

        var battle1 = new Battle
        {
            Id = Guid.NewGuid(), Player1Id = p1.Id, Team1Id = team1.Id,
            Status = BattleStatus.Queued, QueuedAt = DateTime.UtcNow
        };
        var battle2 = new Battle
        {
            Id = Guid.NewGuid(), Player1Id = p2.Id, Team1Id = fakeTeamId,
            Status = BattleStatus.Queued, QueuedAt = DateTime.UtcNow
        };
        _context.Battles.AddRange(battle1, battle2);
        await _context.SaveChangesAsync();

        _matchmaking.Setup(m => m.FindMatchAsync()).ReturnsAsync((battle1, battle2));

        await _service.ProcessQueuedBattlesAsync(CancellationToken.None);

        var updated = await _context.Battles.FindAsync(battle1.Id);
        Assert.Equal(BattleStatus.Cancelled, updated!.Status);
    }

    [Fact]
    public async Task ProcessBattle_Cancellation_RefundsP1()
    {
        var p1 = CreatePlayer("player1");
        p1.DailyBattlesUsed = 3;
        p1.CurrentTier = SubscriptionTier.Free;
        _context.SaveChanges();

        var p2 = CreatePlayer("player2");
        var (team1, _) = CreateTeamWithUnits(p1.Id);

        var battle1 = new Battle
        {
            Id = Guid.NewGuid(), Player1Id = p1.Id, Team1Id = team1.Id,
            Status = BattleStatus.Queued, QueuedAt = DateTime.UtcNow
        };
        var battle2 = new Battle
        {
            Id = Guid.NewGuid(), Player1Id = p2.Id, Team1Id = Guid.NewGuid(), // Nonexistent team → cancel
            Status = BattleStatus.Queued, QueuedAt = DateTime.UtcNow
        };
        _context.Battles.AddRange(battle1, battle2);
        await _context.SaveChangesAsync();

        _matchmaking.Setup(m => m.FindMatchAsync()).ReturnsAsync((battle1, battle2));

        await _service.ProcessQueuedBattlesAsync(CancellationToken.None);

        var updatedP1 = await _context.Players.FindAsync(p1.Id);
        Assert.Equal(2, updatedP1!.DailyBattlesUsed); // Refunded from 3 → 2
    }

    [Fact]
    public async Task ProcessBattle_Cancellation_RefundsP2()
    {
        // BUG REGRESSION: P2 was not getting daily battle refund on cancellation
        var p1 = CreatePlayer("player1");
        var p2 = CreatePlayer("player2");
        p2.DailyBattlesUsed = 5;
        p2.CurrentTier = SubscriptionTier.Free;
        _context.SaveChanges();

        var (team1, _) = CreateTeamWithUnits(p1.Id);

        var battle1 = new Battle
        {
            Id = Guid.NewGuid(), Player1Id = p1.Id, Team1Id = team1.Id,
            Status = BattleStatus.Queued, QueuedAt = DateTime.UtcNow
        };
        var battle2 = new Battle
        {
            Id = Guid.NewGuid(), Player1Id = p2.Id, Team1Id = Guid.NewGuid(), // Nonexistent team → cancel
            Status = BattleStatus.Queued, QueuedAt = DateTime.UtcNow
        };
        _context.Battles.AddRange(battle1, battle2);
        await _context.SaveChangesAsync();

        _matchmaking.Setup(m => m.FindMatchAsync()).ReturnsAsync((battle1, battle2));

        await _service.ProcessQueuedBattlesAsync(CancellationToken.None);

        var updatedP2 = await _context.Players.FindAsync(p2.Id);
        Assert.Equal(4, updatedP2!.DailyBattlesUsed); // Refunded from 5 → 4
    }

    [Fact]
    public async Task ProcessBattle_Winner_GetsPositiveRatingChange()
    {
        var (battle1, _, p1, _) = await SetupMatchedBattle("ranked", 1000, 1000);
        SetupStrategyEngineForWinner(1);

        await _service.ProcessQueuedBattlesAsync(CancellationToken.None);

        var updated = await _context.Battles.FindAsync(battle1.Id);
        Assert.True(updated!.Player1RatingChange > 0, "Winner (P1) should get positive rating change");
    }

    [Fact]
    public async Task ProcessBattle_Loser_RatingFloorAt100()
    {
        // Player 2 starts at rating 100 (minimum) — losing should not go below 100
        var (_, _, _, p2) = await SetupMatchedBattle("ranked", 1500, 100);
        SetupStrategyEngineForWinner(1); // P1 wins, P2 loses

        await _service.ProcessQueuedBattlesAsync(CancellationToken.None);

        var updatedP2 = await _context.Players.FindAsync(p2.Id);
        Assert.True(updatedP2!.Rating >= 100, "Rating should never go below 100");
    }

    [Fact]
    public async Task ProcessBattle_Draw_ZeroRatingChange()
    {
        var (battle1, _, p1, p2) = await SetupMatchedBattle("ranked", 1000, 1000);
        SetupStrategyEngineForWinner(0); // Draw

        await _service.ProcessQueuedBattlesAsync(CancellationToken.None);

        var updated = await _context.Battles.FindAsync(battle1.Id);
        Assert.Null(updated!.WinnerId);
        Assert.Equal(0, updated.Player1RatingChange);
        Assert.Equal(0, updated.Player2RatingChange);

        var updatedP1 = await _context.Players.FindAsync(p1.Id);
        var updatedP2 = await _context.Players.FindAsync(p2.Id);
        Assert.Equal(1000, updatedP1!.Rating);
        Assert.Equal(1000, updatedP2!.Rating);
    }

    [Fact]
    public async Task ProcessBattle_RankedWin_UpdatesSeasonRating()
    {
        var (_, _, p1, p2) = await SetupMatchedBattle("ranked", 1000, 1000);
        SetupStrategyEngineForWinner(1);

        await _service.ProcessQueuedBattlesAsync(CancellationToken.None);

        _season.Verify(s => s.UpdateSeasonRatingAsync(p1.Id, It.Is<int>(r => r > 0), true, false), Times.Once);
        _season.Verify(s => s.UpdateSeasonRatingAsync(p2.Id, It.Is<int>(r => r < 0), false, false), Times.Once);
    }

    [Fact]
    public async Task ProcessBattle_CasualWin_SkipsSeasonRating()
    {
        var (_, _, _, _) = await SetupMatchedBattle("casual", 1000, 1000);
        SetupStrategyEngineForWinner(1);

        await _service.ProcessQueuedBattlesAsync(CancellationToken.None);

        _season.Verify(s => s.UpdateSeasonRatingAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task ProcessBattle_RankedWin_RecordsGuildWarContribution()
    {
        var (_, _, p1, _) = await SetupMatchedBattle("ranked", 1000, 1000);
        SetupStrategyEngineForWinner(1);

        await _service.ProcessQueuedBattlesAsync(CancellationToken.None);

        _guildWar.Verify(g => g.RecordWarContributionAsync(p1.Id, It.IsAny<Guid>()), Times.Once);
    }

    [Fact]
    public async Task ProcessBattle_CasualWin_SkipsGuildWar()
    {
        var (_, _, _, _) = await SetupMatchedBattle("casual", 1000, 1000);
        SetupStrategyEngineForWinner(1);

        await _service.ProcessQueuedBattlesAsync(CancellationToken.None);

        _guildWar.Verify(g => g.RecordWarContributionAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
    }

    // ==================== History & Result Tests ====================

    [Fact]
    public async Task GetBattleHistory_OnlyShowsCompleted()
    {
        var player = CreatePlayer();

        var completed = new Battle
        {
            Id = Guid.NewGuid(), Player1Id = player.Id, Team1Id = Guid.NewGuid(),
            Status = BattleStatus.Completed, BattleLogJson = "[]",
            CompletedAt = DateTime.UtcNow, QueuedAt = DateTime.UtcNow.AddMinutes(-5)
        };
        var cancelled = new Battle
        {
            Id = Guid.NewGuid(), Player1Id = player.Id, Team1Id = Guid.NewGuid(),
            Status = BattleStatus.Cancelled, BattleLogJson = "[]",
            QueuedAt = DateTime.UtcNow.AddMinutes(-10)
        };
        var queued = new Battle
        {
            Id = Guid.NewGuid(), Player1Id = player.Id, Team1Id = Guid.NewGuid(),
            Status = BattleStatus.Queued, BattleLogJson = "[]",
            QueuedAt = DateTime.UtcNow.AddMinutes(-1)
        };

        _context.Battles.AddRange(completed, cancelled, queued);
        await _context.SaveChangesAsync();

        var history = await _service.GetBattleHistoryAsync(player.Id, 10, 0);

        Assert.Single(history);
        Assert.Equal(completed.Id, history[0].BattleId);
    }

    [Fact]
    public async Task GetBattleHistory_RespectsLimitAndOffset()
    {
        var player = CreatePlayer();

        for (int i = 0; i < 5; i++)
        {
            _context.Battles.Add(new Battle
            {
                Id = Guid.NewGuid(), Player1Id = player.Id, Team1Id = Guid.NewGuid(),
                Status = BattleStatus.Completed, BattleLogJson = "[]",
                CompletedAt = DateTime.UtcNow.AddMinutes(-i), QueuedAt = DateTime.UtcNow.AddMinutes(-i - 5)
            });
        }
        await _context.SaveChangesAsync();

        var page1 = await _service.GetBattleHistoryAsync(player.Id, 2, 0);
        var page2 = await _service.GetBattleHistoryAsync(player.Id, 2, 2);

        Assert.Equal(2, page1.Count);
        Assert.Equal(2, page2.Count);
        Assert.NotEqual(page1[0].BattleId, page2[0].BattleId);
    }

    [Fact]
    public async Task GetBattleResult_CancelledBattle_ReturnsStatusCancelled()
    {
        var player = CreatePlayer();
        var battle = new Battle
        {
            Id = Guid.NewGuid(), Player1Id = player.Id, Team1Id = Guid.NewGuid(),
            Status = BattleStatus.Cancelled, BattleLogJson = "[]",
            QueuedAt = DateTime.UtcNow.AddMinutes(-5)
        };
        _context.Battles.Add(battle);
        await _context.SaveChangesAsync();

        var result = await _service.GetBattleResultAsync(battle.Id, player.Id);

        Assert.Equal("cancelled", result.Status);
        Assert.Null(result.Rewards);
    }

    // ==================== Battle Status Tests ====================

    [Fact]
    public async Task GetBattleStatus_QueuedBattle_ReturnsQueuePosition()
    {
        var player = CreatePlayer();
        var (team, _) = CreateTeamWithUnits(player.Id);

        var status = await _service.QueueBattleAsync(player.Id, new BattleQueueRequest { TeamId = team.Id });

        var result = await _service.GetBattleStatusAsync(status.BattleId, player.Id);

        Assert.Equal("queued", result.Status);
        Assert.NotNull(result.QueuePosition);
        Assert.NotNull(result.EstimatedWaitSeconds);
    }

    [Fact]
    public async Task GetBattleStatus_OtherPlayersBattle_ThrowsNotFound()
    {
        var player1 = CreatePlayer("player1");
        var player2 = CreatePlayer("player2");
        var (team, _) = CreateTeamWithUnits(player1.Id);

        var status = await _service.QueueBattleAsync(player1.Id, new BattleQueueRequest { TeamId = team.Id });

        // Player2 tries to view Player1's battle
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.GetBattleStatusAsync(status.BattleId, player2.Id));
    }

    // ==================== Exception Handling ====================

    [Fact]
    public async Task ProcessBattle_ExceptionDuringResolution_CancelsAndRefundsBoth()
    {
        var p1 = CreatePlayer("player1");
        var p2 = CreatePlayer("player2");
        p1.DailyBattlesUsed = 2;
        p2.DailyBattlesUsed = 3;
        _context.SaveChanges();

        var (team1, _) = CreateTeamWithUnits(p1.Id);
        var (team2, _) = CreateTeamWithUnits(p2.Id);

        var battle1 = new Battle
        {
            Id = Guid.NewGuid(), Player1Id = p1.Id, Team1Id = team1.Id,
            Status = BattleStatus.Queued, QueuedAt = DateTime.UtcNow
        };
        var battle2 = new Battle
        {
            Id = Guid.NewGuid(), Player1Id = p2.Id, Team1Id = team2.Id,
            Status = BattleStatus.Queued, QueuedAt = DateTime.UtcNow
        };
        _context.Battles.AddRange(battle1, battle2);
        await _context.SaveChangesAsync();

        _matchmaking.Setup(m => m.FindMatchAsync()).ReturnsAsync((battle1, battle2));

        // Strategy engine throws
        _strategyEngine.Setup(s => s.ResolveBattle(
                It.IsAny<List<Unit>>(), It.IsAny<StrategyConfig>(),
                It.IsAny<List<Unit>>(), It.IsAny<StrategyConfig>(),
                It.IsAny<int>(), It.IsAny<int?>()))
            .Throws(new InvalidOperationException("Something broke"));

        await _service.ProcessQueuedBattlesAsync(CancellationToken.None);

        var updated = await _context.Battles.FindAsync(battle1.Id);
        Assert.Equal(BattleStatus.Cancelled, updated!.Status);

        var updatedP1 = await _context.Players.FindAsync(p1.Id);
        var updatedP2 = await _context.Players.FindAsync(p2.Id);
        Assert.Equal(1, updatedP1!.DailyBattlesUsed); // Refunded
        Assert.Equal(2, updatedP2!.DailyBattlesUsed); // Refunded
    }
}
