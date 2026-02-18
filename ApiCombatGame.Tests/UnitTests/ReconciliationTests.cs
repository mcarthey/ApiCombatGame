using ApiCombatGame.Data;
using ApiCombatGame.Models.Domain;
using ApiCombatGame.Models.Enums;
using ApiCombatGame.Services;
using ApiCombatGame.Services.Interfaces;
using ApiCombatGame.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ApiCombatGame.Tests.UnitTests;

public class ReconciliationTests : IDisposable
{
    private readonly GameDbContext _context;
    private readonly Mock<INotificationService> _notifications;
    private readonly Mock<ILogger<AdminAnalyticsService>> _logger;
    private readonly AdminAnalyticsService _service;

    public ReconciliationTests()
    {
        _context = TestDbContextFactory.Create();
        _notifications = new Mock<INotificationService>();
        _logger = new Mock<ILogger<AdminAnalyticsService>>();
        _service = new AdminAnalyticsService(_context, _notifications.Object, new Mock<IActivityLedger>().Object, _logger.Object);
    }

    public void Dispose() => _context.Dispose();

    private Battle CreateBattle(Guid p1Id, Guid p2Id, Guid? winnerId, string mode,
        int? p1RatingChange, int? p2RatingChange, DateTime completedAt)
    {
        var battle = new Battle
        {
            Id = Guid.NewGuid(),
            Player1Id = p1Id,
            Player2Id = p2Id,
            Team1Id = Guid.NewGuid(),
            Team2Id = Guid.NewGuid(),
            WinnerId = winnerId,
            Mode = mode,
            Status = BattleStatus.Completed,
            Player1RatingChange = p1RatingChange,
            Player2RatingChange = p2RatingChange,
            CompletedAt = completedAt,
            QueuedAt = completedAt.AddMinutes(-1)
        };
        _context.Battles.Add(battle);
        return battle;
    }

    [Fact]
    public async Task Preview_CasualBattleWithRatingChange_DetectsDiscrepancy()
    {
        var p1 = TestDbContextFactory.CreatePlayer(_context, "player1");
        var p2 = TestDbContextFactory.CreatePlayer(_context, "player2");
        p1.Rating = 1016; // Buggy: casual battle applied ELO
        p2.Rating = 984;
        CreateBattle(p1.Id, p2.Id, p1.Id, "casual", 16, -16, DateTime.UtcNow.AddHours(-1));
        await _context.SaveChangesAsync();

        var preview = await _service.PreviewReconciliationAsync();

        Assert.Equal(2, preview.TotalPlayersAffected);
        Assert.Equal(1, preview.CasualBattlesFixed);
        var p1Delta = preview.PlayerDeltas.First(d => d.PlayerId == p1.Id);
        Assert.Equal(1000, p1Delta.RecalculatedRating);
        Assert.Equal(-16, p1Delta.Delta);
    }

    [Fact]
    public async Task Preview_CorrectRankedBattle_NoDiscrepancy()
    {
        var p1 = TestDbContextFactory.CreatePlayer(_context, "player1");
        var p2 = TestDbContextFactory.CreatePlayer(_context, "player2");
        // Both start at 1000, p1 wins: expected=0.5, winnerChange=(int)(32*0.5)=16
        p1.Rating = 1016;
        p2.Rating = 984;
        CreateBattle(p1.Id, p2.Id, p1.Id, "ranked", 16, -16, DateTime.UtcNow);
        await _context.SaveChangesAsync();

        var preview = await _service.PreviewReconciliationAsync();

        Assert.Equal(0, preview.TotalPlayersAffected);
        Assert.Equal(0, preview.CasualBattlesFixed);
    }

    [Fact]
    public async Task Execute_FixesCasualBugAndAuditsAndNotifies()
    {
        var admin = TestDbContextFactory.CreatePlayer(_context, "admin");
        admin.IsAdmin = true;
        var p1 = TestDbContextFactory.CreatePlayer(_context, "player1");
        var p2 = TestDbContextFactory.CreatePlayer(_context, "player2");
        p1.Rating = 1016;
        p2.Rating = 984;
        var battle = CreateBattle(p1.Id, p2.Id, p1.Id, "casual", 16, -16, DateTime.UtcNow);
        await _context.SaveChangesAsync();

        var result = await _service.ExecuteReconciliationAsync(admin.Id);

        // Ratings fixed
        Assert.Equal(1000, p1.Rating);
        Assert.Equal(1000, p2.Rating);

        // Battle record fixed
        Assert.Equal(0, battle.Player1RatingChange);
        Assert.Equal(0, battle.Player2RatingChange);

        // Audit logs created
        var auditLogs = _context.AdminAuditLogs.Where(l => l.Action.Contains("Reconcile")).ToList();
        Assert.True(auditLogs.Count >= 1);

        // Notifications sent to both players
        _notifications.Verify(n => n.SendAsync(
            p1.Id, NotificationType.AdminActionOnAccount,
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        _notifications.Verify(n => n.SendAsync(
            p2.Id, NotificationType.AdminActionOnAccount,
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Execute_RatingFloorMaintainedAt100()
    {
        var admin = TestDbContextFactory.CreatePlayer(_context, "admin");
        var p1 = TestDbContextFactory.CreatePlayer(_context, "winner");
        var p2 = TestDbContextFactory.CreatePlayer(_context, "loser");
        p1.Rating = 1200;
        p2.Rating = 100;

        // Create many ranked battles where p1 always wins to push p2 below 100
        for (int i = 0; i < 50; i++)
        {
            CreateBattle(p1.Id, p2.Id, p1.Id, "ranked", 1, -1,
                DateTime.UtcNow.AddMinutes(-50 + i));
        }
        await _context.SaveChangesAsync();

        await _service.ExecuteReconciliationAsync(admin.Id);

        Assert.True(p2.Rating >= 100, $"Rating floor violated: {p2.Rating}");
    }

    [Fact]
    public async Task PreviewPlayer_ReturnsOnlyTargetPlayer()
    {
        var p1 = TestDbContextFactory.CreatePlayer(_context, "player1");
        var p2 = TestDbContextFactory.CreatePlayer(_context, "player2");
        p1.Rating = 1016;
        p2.Rating = 984;
        CreateBattle(p1.Id, p2.Id, p1.Id, "casual", 16, -16, DateTime.UtcNow);
        await _context.SaveChangesAsync();

        var preview = await _service.PreviewPlayerReconciliationAsync(p1.Id);

        Assert.Single(preview.PlayerDeltas);
        Assert.Equal(p1.Id, preview.PlayerDeltas[0].PlayerId);
    }

    [Fact]
    public async Task Since_OnlyReplaysBattlesAfterCutoff()
    {
        var p1 = TestDbContextFactory.CreatePlayer(_context, "player1");
        var p2 = TestDbContextFactory.CreatePlayer(_context, "player2");

        var earlyTime = DateTime.UtcNow.AddDays(-5);
        var lateTime = DateTime.UtcNow.AddHours(-1);
        var cutoff = DateTime.UtcNow.AddDays(-2);

        // Early battle: ranked, p1 wins (before cutoff — trusted)
        CreateBattle(p1.Id, p2.Id, p1.Id, "ranked", 16, -16, earlyTime);

        // Late battle: casual with buggy rating change (after cutoff — replayed)
        var lateBattle = CreateBattle(p1.Id, p2.Id, p1.Id, "casual", 16, -16, lateTime);

        // Set player ratings as if both battles applied ELO
        p1.Rating = 1032; // 1000 + 16 + 16
        p2.Rating = 968;  // 1000 - 16 - 16
        await _context.SaveChangesAsync();

        var preview = await _service.PreviewReconciliationAsync(since: cutoff);

        // Only the late casual battle should be fixed
        Assert.Equal(1, preview.CasualBattlesFixed);
        Assert.Equal(1, preview.TotalBattlesReprocessed); // Only 1 battle replayed

        // p1 should be 1016 (1000 + 16 from trusted early battle + 0 from casual)
        var p1Delta = preview.PlayerDeltas.First(d => d.PlayerId == p1.Id);
        Assert.Equal(1016, p1Delta.RecalculatedRating);
        Assert.Equal(-16, p1Delta.Delta); // 1016 - 1032
    }

    [Fact]
    public async Task Since_PreSinceBattleRecordsUntouched()
    {
        var admin = TestDbContextFactory.CreatePlayer(_context, "admin");
        var p1 = TestDbContextFactory.CreatePlayer(_context, "player1");
        var p2 = TestDbContextFactory.CreatePlayer(_context, "player2");

        var earlyTime = DateTime.UtcNow.AddDays(-5);
        var lateTime = DateTime.UtcNow.AddHours(-1);
        var cutoff = DateTime.UtcNow.AddDays(-2);

        // Early casual battle with buggy rating (before cutoff — should NOT be touched)
        var earlyBattle = CreateBattle(p1.Id, p2.Id, p1.Id, "casual", 10, -10, earlyTime);

        // Late casual battle with buggy rating (after cutoff — should be fixed)
        var lateBattle = CreateBattle(p1.Id, p2.Id, p1.Id, "casual", 16, -16, lateTime);

        p1.Rating = 1026;
        p2.Rating = 974;
        await _context.SaveChangesAsync();

        await _service.ExecuteReconciliationAsync(admin.Id, since: cutoff);

        // Early battle should be untouched (trusted, even though it has buggy data)
        Assert.Equal(10, earlyBattle.Player1RatingChange);
        Assert.Equal(-10, earlyBattle.Player2RatingChange);

        // Late battle should be fixed
        Assert.Equal(0, lateBattle.Player1RatingChange);
        Assert.Equal(0, lateBattle.Player2RatingChange);
    }

    [Fact]
    public async Task FullReplay_SameAsSinceBeforeAllBattles()
    {
        var p1 = TestDbContextFactory.CreatePlayer(_context, "player1");
        var p2 = TestDbContextFactory.CreatePlayer(_context, "player2");
        p1.Rating = 1016;
        p2.Rating = 984;

        CreateBattle(p1.Id, p2.Id, p1.Id, "casual", 16, -16, DateTime.UtcNow.AddHours(-1));
        await _context.SaveChangesAsync();

        var fullPreview = await _service.PreviewReconciliationAsync(since: null);
        var scopedPreview = await _service.PreviewReconciliationAsync(since: DateTime.UtcNow.AddDays(-30));

        Assert.Equal(fullPreview.TotalPlayersAffected, scopedPreview.TotalPlayersAffected);
        Assert.Equal(fullPreview.CasualBattlesFixed, scopedPreview.CasualBattlesFixed);

        // Both should detect the same delta for p1
        var fullP1 = fullPreview.PlayerDeltas.First(d => d.PlayerId == p1.Id);
        var scopedP1 = scopedPreview.PlayerDeltas.First(d => d.PlayerId == p1.Id);
        Assert.Equal(fullP1.RecalculatedRating, scopedP1.RecalculatedRating);
    }
}
