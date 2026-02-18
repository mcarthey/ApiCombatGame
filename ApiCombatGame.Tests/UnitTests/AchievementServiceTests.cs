using ApiCombatGame.Data;
using ApiCombatGame.Models.Domain;
using ApiCombatGame.Models.Enums;
using ApiCombatGame.Services;
using ApiCombatGame.Services.Interfaces;
using ApiCombatGame.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace ApiCombatGame.Tests.UnitTests;

public class AchievementServiceTests
{
    private readonly Mock<IPlayerProgressionService> _progressionMock;
    private readonly Mock<INotificationService> _notificationsMock;
    private readonly Mock<IActivityLedger> _ledgerMock;
    private readonly NullLogger<AchievementService> _logger;

    public AchievementServiceTests()
    {
        _progressionMock = new Mock<IPlayerProgressionService>();
        _notificationsMock = new Mock<INotificationService>();
        _ledgerMock = new Mock<IActivityLedger>();
        _logger = NullLogger<AchievementService>.Instance;
    }

    private AchievementService CreateService(GameDbContext context)
    {
        return new AchievementService(context, _progressionMock.Object, _logger, _notificationsMock.Object, _ledgerMock.Object);
    }

    /// <summary>Seeds an achievement into the database and returns it.</summary>
    private static Achievement SeedAchievement(
        GameDbContext context,
        string name = "Test Achievement",
        string eventType = "battle_won",
        int count = 1,
        int points = 10,
        int? rewardCurrency = null,
        string category = "Combat",
        bool isSecret = false)
    {
        var rewardPart = rewardCurrency.HasValue
            ? $", \"rewardCurrency\": {rewardCurrency.Value}"
            : "";

        var achievement = new Achievement
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = $"Test: {name}",
            IconUrl = "",
            Category = category,
            Points = points,
            RequirementsJson = $"{{\"eventType\": \"{eventType}\", \"count\": {count}{rewardPart}}}",
            IsSecret = isSecret,
            CreatedAt = DateTime.UtcNow
        };
        context.Achievements.Add(achievement);
        context.SaveChanges();
        return achievement;
    }

    // ---------------------------------------------------------------
    // GetPlayerAchievementsAsync
    // ---------------------------------------------------------------

    [Fact]
    public async Task GetPlayerAchievements_NewPlayer_ReturnsEmptyList()
    {
        using var context = TestDbContextFactory.Create();
        var player = TestDbContextFactory.CreatePlayer(context, "newbie");
        var service = CreateService(context);

        var results = await service.GetPlayerAchievementsAsync(player.Id);

        Assert.Empty(results);
    }

    [Fact]
    public async Task GetPlayerAchievements_ReturnsOrderedByUnlockedAtDescending()
    {
        using var context = TestDbContextFactory.Create();
        var player = TestDbContextFactory.CreatePlayer(context, "collector");
        var ach1 = SeedAchievement(context, "First", "battle_won", points: 5);
        var ach2 = SeedAchievement(context, "Second", "guild_joined", points: 10);
        var ach3 = SeedAchievement(context, "Third", "unit_unlocked", points: 15);

        // Create player achievements with staggered unlock times
        var pa1 = new PlayerAchievement
        {
            Id = Guid.NewGuid(),
            PlayerId = player.Id,
            AchievementId = ach1.Id,
            Progress = 1,
            RequiredProgress = 1,
            IsUnlocked = true,
            UnlockedAt = DateTime.UtcNow.AddHours(-3) // oldest
        };
        var pa2 = new PlayerAchievement
        {
            Id = Guid.NewGuid(),
            PlayerId = player.Id,
            AchievementId = ach2.Id,
            Progress = 1,
            RequiredProgress = 1,
            IsUnlocked = true,
            UnlockedAt = DateTime.UtcNow.AddHours(-1) // newest
        };
        var pa3 = new PlayerAchievement
        {
            Id = Guid.NewGuid(),
            PlayerId = player.Id,
            AchievementId = ach3.Id,
            Progress = 3,
            RequiredProgress = 5,
            IsUnlocked = false,
            UnlockedAt = null // not unlocked yet
        };
        context.PlayerAchievements.AddRange(pa1, pa2, pa3);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var results = await service.GetPlayerAchievementsAsync(player.Id);

        Assert.Equal(3, results.Count);
        // Ordered by UnlockedAt descending: pa2 (newest), pa1 (oldest), pa3 (null last)
        Assert.Equal(ach2.Id, results[0].AchievementId);
        Assert.Equal(ach1.Id, results[1].AchievementId);
        Assert.Equal(ach3.Id, results[2].AchievementId);
    }

    // ---------------------------------------------------------------
    // CheckAndAwardAsync - Progress creation
    // ---------------------------------------------------------------

    [Fact]
    public async Task CheckAndAward_FirstMatchingEvent_CreatesProgressRecord()
    {
        using var context = TestDbContextFactory.Create();
        var player = TestDbContextFactory.CreatePlayer(context, "first_event");
        var achievement = SeedAchievement(context, "Win 10 Battles", "battle_won", count: 10, points: 50);
        var service = CreateService(context);

        await service.CheckAndAwardAsync(player.Id, "battle_won");

        var pa = context.PlayerAchievements
            .SingleOrDefault(pa => pa.PlayerId == player.Id && pa.AchievementId == achievement.Id);

        Assert.NotNull(pa);
        Assert.Equal(1, pa.Progress);
        Assert.Equal(10, pa.RequiredProgress);
        Assert.False(pa.IsUnlocked);
        Assert.Null(pa.UnlockedAt);
    }

    [Fact]
    public async Task CheckAndAward_SubsequentEvents_IncrementsProgress()
    {
        using var context = TestDbContextFactory.Create();
        var player = TestDbContextFactory.CreatePlayer(context, "incrementer");
        SeedAchievement(context, "Win 5 Battles", "battle_won", count: 5, points: 25);
        var service = CreateService(context);

        // Fire the event 3 times
        await service.CheckAndAwardAsync(player.Id, "battle_won");
        await service.CheckAndAwardAsync(player.Id, "battle_won");
        await service.CheckAndAwardAsync(player.Id, "battle_won");

        var pa = context.PlayerAchievements
            .Single(pa => pa.PlayerId == player.Id);

        Assert.Equal(3, pa.Progress);
        Assert.Equal(5, pa.RequiredProgress);
        Assert.False(pa.IsUnlocked);
    }

    // ---------------------------------------------------------------
    // CheckAndAwardAsync - Unlocking
    // ---------------------------------------------------------------

    [Fact]
    public async Task CheckAndAward_ProgressMeetsThreshold_UnlocksAchievement()
    {
        using var context = TestDbContextFactory.Create();
        var player = TestDbContextFactory.CreatePlayer(context, "unlocker");
        var achievement = SeedAchievement(context, "Join a Guild", "guild_joined", count: 1, points: 20);
        var service = CreateService(context);

        await service.CheckAndAwardAsync(player.Id, "guild_joined");

        var pa = context.PlayerAchievements
            .Single(pa => pa.PlayerId == player.Id && pa.AchievementId == achievement.Id);

        Assert.True(pa.IsUnlocked);
        Assert.NotNull(pa.UnlockedAt);
        Assert.Equal(1, pa.Progress);
        Assert.Equal(1, pa.RequiredProgress);
    }

    [Fact]
    public async Task CheckAndAward_Unlock_AwardsAchievementPointsToPlayer()
    {
        using var context = TestDbContextFactory.Create();
        var player = TestDbContextFactory.CreatePlayer(context, "pointer");
        SeedAchievement(context, "First Blood", "battle_won", count: 1, points: 100);
        var service = CreateService(context);

        Assert.Equal(0, player.AchievementPoints);

        await service.CheckAndAwardAsync(player.Id, "battle_won");

        await context.Entry(player).ReloadAsync();
        Assert.Equal(100, player.AchievementPoints);
    }

    [Fact]
    public async Task CheckAndAward_Unlock_CallsAwardCurrencyWhenRewardCurrencySpecified()
    {
        using var context = TestDbContextFactory.Create();
        var player = TestDbContextFactory.CreatePlayer(context, "goldwinner");
        SeedAchievement(context, "Win 10 Battles", "battle_won", count: 1, points: 50, rewardCurrency: 500);

        _progressionMock
            .Setup(p => p.AwardCurrencyAsync(player.Id, 500))
            .ReturnsAsync(500);

        var service = CreateService(context);

        await service.CheckAndAwardAsync(player.Id, "battle_won");

        _progressionMock.Verify(
            p => p.AwardCurrencyAsync(player.Id, 500),
            Times.Once);
    }

    [Fact]
    public async Task CheckAndAward_Unlock_SendsNotification()
    {
        using var context = TestDbContextFactory.Create();
        var player = TestDbContextFactory.CreatePlayer(context, "notified");
        var achievement = SeedAchievement(context, "First Victory", "battle_won", count: 1, points: 30);
        var service = CreateService(context);

        await service.CheckAndAwardAsync(player.Id, "battle_won");

        _notificationsMock.Verify(
            n => n.SendAsync(
                player.Id,
                NotificationType.AchievementUnlocked,
                "Achievement Unlocked!",
                It.Is<string>(msg => msg.Contains("First Victory") && msg.Contains("30")),
                It.IsAny<string?>()),
            Times.Once);
    }

    [Fact]
    public async Task CheckAndAward_Unlock_DoesNotCallAwardCurrencyWhenNoRewardCurrency()
    {
        using var context = TestDbContextFactory.Create();
        var player = TestDbContextFactory.CreatePlayer(context, "nogold");
        SeedAchievement(context, "Join a Guild", "guild_joined", count: 1, points: 15);
        var service = CreateService(context);

        await service.CheckAndAwardAsync(player.Id, "guild_joined");

        _progressionMock.Verify(
            p => p.AwardCurrencyAsync(It.IsAny<Guid>(), It.IsAny<int>()),
            Times.Never);
    }

    // ---------------------------------------------------------------
    // CheckAndAwardAsync - Skip / Ignore scenarios
    // ---------------------------------------------------------------

    [Fact]
    public async Task CheckAndAward_SkipsAlreadyUnlockedAchievements()
    {
        using var context = TestDbContextFactory.Create();
        var player = TestDbContextFactory.CreatePlayer(context, "skipped");
        var achievement = SeedAchievement(context, "Already Done", "battle_won", count: 1, points: 10);

        // Pre-create an already-unlocked record
        context.PlayerAchievements.Add(new PlayerAchievement
        {
            Id = Guid.NewGuid(),
            PlayerId = player.Id,
            AchievementId = achievement.Id,
            Progress = 1,
            RequiredProgress = 1,
            IsUnlocked = true,
            UnlockedAt = DateTime.UtcNow.AddDays(-1)
        });
        await context.SaveChangesAsync();

        player.AchievementPoints = 10;
        await context.SaveChangesAsync();

        var service = CreateService(context);

        await service.CheckAndAwardAsync(player.Id, "battle_won");

        // Points should not increase (still 10, not 20)
        await context.Entry(player).ReloadAsync();
        Assert.Equal(10, player.AchievementPoints);

        // Notification should not be sent again
        _notificationsMock.Verify(
            n => n.SendAsync(It.IsAny<Guid>(), NotificationType.AchievementUnlocked, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()),
            Times.Never);
    }

    [Fact]
    public async Task CheckAndAward_IgnoresAchievementsWithNonMatchingEventType()
    {
        using var context = TestDbContextFactory.Create();
        var player = TestDbContextFactory.CreatePlayer(context, "mismatch");
        SeedAchievement(context, "Guild Achievement", "guild_joined", count: 1, points: 20);
        var service = CreateService(context);

        // Fire a different event type
        await service.CheckAndAwardAsync(player.Id, "battle_won");

        // No progress record should be created
        var records = context.PlayerAchievements
            .Where(pa => pa.PlayerId == player.Id)
            .ToList();

        Assert.Empty(records);
    }

    [Fact]
    public async Task CheckAndAward_NonexistentPlayer_DoesNothing()
    {
        using var context = TestDbContextFactory.Create();
        SeedAchievement(context, "Some Achievement", "battle_won", count: 1, points: 10);
        var service = CreateService(context);

        var fakePlayerId = Guid.NewGuid();

        // Should not throw
        await service.CheckAndAwardAsync(fakePlayerId, "battle_won");

        // No records created
        Assert.Empty(context.PlayerAchievements.ToList());
    }

    // ---------------------------------------------------------------
    // CheckAndAwardAsync - Multi-achievement scenarios
    // ---------------------------------------------------------------

    [Fact]
    public async Task CheckAndAward_MultipleMatchingAchievements_ProgressesAll()
    {
        using var context = TestDbContextFactory.Create();
        var player = TestDbContextFactory.CreatePlayer(context, "multi");
        var ach1 = SeedAchievement(context, "Win 1 Battle", "battle_won", count: 1, points: 10);
        var ach2 = SeedAchievement(context, "Win 5 Battles", "battle_won", count: 5, points: 50);
        var ach3 = SeedAchievement(context, "Win 10 Battles", "battle_won", count: 10, points: 100);
        var service = CreateService(context);

        await service.CheckAndAwardAsync(player.Id, "battle_won");

        var records = context.PlayerAchievements
            .Where(pa => pa.PlayerId == player.Id)
            .ToList();

        Assert.Equal(3, records.Count);

        // First should be unlocked (count: 1)
        var pa1 = records.Single(r => r.AchievementId == ach1.Id);
        Assert.True(pa1.IsUnlocked);
        Assert.Equal(1, pa1.Progress);

        // Other two should have progress but not be unlocked
        var pa2 = records.Single(r => r.AchievementId == ach2.Id);
        Assert.False(pa2.IsUnlocked);
        Assert.Equal(1, pa2.Progress);
        Assert.Equal(5, pa2.RequiredProgress);

        var pa3 = records.Single(r => r.AchievementId == ach3.Id);
        Assert.False(pa3.IsUnlocked);
        Assert.Equal(1, pa3.Progress);
        Assert.Equal(10, pa3.RequiredProgress);
    }

    [Fact]
    public async Task CheckAndAward_MultipleUnlocks_AwardsPointsForEach()
    {
        using var context = TestDbContextFactory.Create();
        var player = TestDbContextFactory.CreatePlayer(context, "multipoints");
        SeedAchievement(context, "Quick Win", "battle_won", count: 1, points: 25, rewardCurrency: 100);
        SeedAchievement(context, "Instant Win", "battle_won", count: 1, points: 50, rewardCurrency: 200);

        _progressionMock
            .Setup(p => p.AwardCurrencyAsync(player.Id, It.IsAny<int>()))
            .ReturnsAsync((Guid _, int amount) => amount);

        var service = CreateService(context);

        await service.CheckAndAwardAsync(player.Id, "battle_won");

        // Both achievements should be unlocked, totaling 75 points
        await context.Entry(player).ReloadAsync();
        Assert.Equal(75, player.AchievementPoints);

        // Currency should be awarded for both
        _progressionMock.Verify(p => p.AwardCurrencyAsync(player.Id, 100), Times.Once);
        _progressionMock.Verify(p => p.AwardCurrencyAsync(player.Id, 200), Times.Once);

        // Notifications sent for both
        _notificationsMock.Verify(
            n => n.SendAsync(player.Id, NotificationType.AchievementUnlocked, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task CheckAndAward_ProgressiveUnlock_UnlocksOnExactThreshold()
    {
        using var context = TestDbContextFactory.Create();
        var player = TestDbContextFactory.CreatePlayer(context, "progressive");
        var achievement = SeedAchievement(context, "Unlock 5 Units", "unit_unlocked", count: 5, points: 40);
        var service = CreateService(context);

        // Progress through 4 events - should not unlock
        for (int i = 0; i < 4; i++)
        {
            await service.CheckAndAwardAsync(player.Id, "unit_unlocked");
        }

        var pa = context.PlayerAchievements
            .Single(r => r.PlayerId == player.Id && r.AchievementId == achievement.Id);
        Assert.Equal(4, pa.Progress);
        Assert.False(pa.IsUnlocked);

        // 5th event - should unlock
        await service.CheckAndAwardAsync(player.Id, "unit_unlocked");

        await context.Entry(pa).ReloadAsync();
        Assert.Equal(5, pa.Progress);
        Assert.True(pa.IsUnlocked);
        Assert.NotNull(pa.UnlockedAt);

        await context.Entry(player).ReloadAsync();
        Assert.Equal(40, player.AchievementPoints);
    }
}
