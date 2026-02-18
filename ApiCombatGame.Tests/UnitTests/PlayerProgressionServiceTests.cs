using ApiCombatGame.Data;
using ApiCombatGame.Models.Domain;
using ApiCombatGame.Models.DTOs.Progression;
using ApiCombatGame.Models.Enums;
using ApiCombatGame.Services;
using ApiCombatGame.Services.Interfaces;
using ApiCombatGame.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace ApiCombatGame.Tests.UnitTests;

public class PlayerProgressionServiceTests
{
    private readonly Mock<INotificationService> _notificationsMock;
    private readonly NullLogger<PlayerProgressionService> _logger;

    public PlayerProgressionServiceTests()
    {
        _notificationsMock = new Mock<INotificationService>();
        _logger = NullLogger<PlayerProgressionService>.Instance;
    }

    private PlayerProgressionService CreateService(GameDbContext context)
    {
        return new PlayerProgressionService(context, _logger, _notificationsMock.Object, new ActivityLedger(context));
    }

    // ---------------------------------------------------------------
    // ProcessBattleRewardsAsync - Win / Loss basics
    // ---------------------------------------------------------------

    [Fact]
    public async Task ProcessBattleRewards_Win_GrantsBaseGoldAndXp()
    {
        using var context = TestDbContextFactory.Create();
        var player = TestDbContextFactory.CreatePlayer(context, "winner", SubscriptionTier.Free, currency: 0, level: 1);
        var service = CreateService(context);

        var summary = await service.ProcessBattleRewardsAsync(player.Id, won: true, ratingChange: 25);

        // Win base = 50, first win streak = 1*10 = 10, first battle of day = 100
        // Total base = 50 + 10 + 100 = 160, multiplier 1.0 => 160 gold
        Assert.Equal(100, summary.XpEarned);
        Assert.True(summary.GoldEarned >= 50, "Win should grant at least 50 base gold (before bonuses).");
    }

    [Fact]
    public async Task ProcessBattleRewards_Loss_GrantsReducedGoldAndXp()
    {
        using var context = TestDbContextFactory.Create();
        var player = TestDbContextFactory.CreatePlayer(context, "loser", SubscriptionTier.Free, currency: 0, level: 1);
        // Set LastFirstBattleBonus to today so first-battle bonus does not interfere
        player.LastFirstBattleBonus = DateTime.UtcNow;
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var summary = await service.ProcessBattleRewardsAsync(player.Id, won: false, ratingChange: -15);

        // Loss base = 10, no streak bonus, no first battle bonus, multiplier 1.0
        Assert.Equal(10, summary.GoldEarned);
        Assert.Equal(25, summary.XpEarned);
        Assert.Equal(0, summary.WinStreak);
    }

    // ---------------------------------------------------------------
    // Win streak
    // ---------------------------------------------------------------

    [Fact]
    public async Task ProcessBattleRewards_WinStreak_IncrementsOnConsecutiveWins()
    {
        using var context = TestDbContextFactory.Create();
        var player = TestDbContextFactory.CreatePlayer(context, "streaker", SubscriptionTier.Free, currency: 0, level: 1);
        var service = CreateService(context);

        await service.ProcessBattleRewardsAsync(player.Id, won: true, ratingChange: 10);
        var summary2 = await service.ProcessBattleRewardsAsync(player.Id, won: true, ratingChange: 10);
        var summary3 = await service.ProcessBattleRewardsAsync(player.Id, won: true, ratingChange: 10);

        Assert.Equal(3, summary3.WinStreak);
        Assert.Equal(30, summary3.WinStreakBonus); // 3 * 10
    }

    [Fact]
    public async Task ProcessBattleRewards_WinStreak_ResetsOnLoss()
    {
        using var context = TestDbContextFactory.Create();
        var player = TestDbContextFactory.CreatePlayer(context, "resetter", SubscriptionTier.Free, currency: 0, level: 1);
        var service = CreateService(context);

        // Build a 2-win streak
        await service.ProcessBattleRewardsAsync(player.Id, won: true, ratingChange: 10);
        await service.ProcessBattleRewardsAsync(player.Id, won: true, ratingChange: 10);

        // Lose
        var lossSummary = await service.ProcessBattleRewardsAsync(player.Id, won: false, ratingChange: -10);
        Assert.Equal(0, lossSummary.WinStreak);
        Assert.Equal(0, lossSummary.WinStreakBonus);

        // Verify player entity was updated
        await context.Entry(player).ReloadAsync();
        Assert.Equal(0, player.WinStreak);
    }

    // ---------------------------------------------------------------
    // First battle of day bonus
    // ---------------------------------------------------------------

    [Fact]
    public async Task ProcessBattleRewards_FirstBattleOfDay_AppliesBonus()
    {
        using var context = TestDbContextFactory.Create();
        var player = TestDbContextFactory.CreatePlayer(context, "daily", SubscriptionTier.Free, currency: 0, level: 1);
        // Ensure no previous first-battle bonus recorded
        player.LastFirstBattleBonus = null;
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var summary = await service.ProcessBattleRewardsAsync(player.Id, won: false, ratingChange: -5);

        Assert.True(summary.FirstBattleBonus);
        // Loss base=10 + first battle=100 = 110, multiplier 1.0 => 110
        Assert.Equal(110, summary.GoldEarned);
    }

    [Fact]
    public async Task ProcessBattleRewards_FirstBattleBonus_NotAppliedTwiceSameDay()
    {
        using var context = TestDbContextFactory.Create();
        var player = TestDbContextFactory.CreatePlayer(context, "twobattles", SubscriptionTier.Free, currency: 0, level: 1);
        player.LastFirstBattleBonus = null;
        await context.SaveChangesAsync();
        var service = CreateService(context);

        // First battle of day - should get bonus
        var summary1 = await service.ProcessBattleRewardsAsync(player.Id, won: false, ratingChange: -5);
        Assert.True(summary1.FirstBattleBonus);

        // Second battle same day - should not get bonus
        var summary2 = await service.ProcessBattleRewardsAsync(player.Id, won: false, ratingChange: -5);
        Assert.False(summary2.FirstBattleBonus);
        // Loss base=10, no first battle, multiplier 1.0 => 10
        Assert.Equal(10, summary2.GoldEarned);
    }

    // ---------------------------------------------------------------
    // Tier multipliers
    // ---------------------------------------------------------------

    [Fact]
    public async Task ProcessBattleRewards_PremiumTier_Applies1_5xMultiplier()
    {
        using var context = TestDbContextFactory.Create();
        var player = TestDbContextFactory.CreatePlayer(context, "premium", SubscriptionTier.Premium, currency: 0, level: 1);
        player.LastFirstBattleBonus = DateTime.UtcNow; // Suppress first-battle bonus
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var summary = await service.ProcessBattleRewardsAsync(player.Id, won: false, ratingChange: -5);

        Assert.Equal(1.5m, summary.TierMultiplier);
        // Loss base=10, no streak, no first battle => 10 * 1.5 = 15
        Assert.Equal(15, summary.GoldEarned);
    }

    [Fact]
    public async Task ProcessBattleRewards_PremiumPlusTier_Applies2xMultiplier()
    {
        using var context = TestDbContextFactory.Create();
        var player = TestDbContextFactory.CreatePlayer(context, "premplus", SubscriptionTier.PremiumPlus, currency: 0, level: 1);
        player.LastFirstBattleBonus = DateTime.UtcNow; // Suppress first-battle bonus
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var summary = await service.ProcessBattleRewardsAsync(player.Id, won: false, ratingChange: -5);

        Assert.Equal(2.0m, summary.TierMultiplier);
        // Loss base=10, no streak, no first battle => 10 * 2.0 = 20
        Assert.Equal(20, summary.GoldEarned);
    }

    // ---------------------------------------------------------------
    // Level up
    // ---------------------------------------------------------------

    [Fact]
    public async Task ProcessBattleRewards_LevelUp_TriggersAtXpThreshold()
    {
        using var context = TestDbContextFactory.Create();
        var player = TestDbContextFactory.CreatePlayer(context, "levelup", SubscriptionTier.Free, currency: 0, level: 1);
        player.LastFirstBattleBonus = DateTime.UtcNow;
        // Level 1 needs 750 XP. Give 700 so next win (100 XP) triggers level up.
        player.ExperiencePoints = 700;
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var summary = await service.ProcessBattleRewardsAsync(player.Id, won: true, ratingChange: 20);

        Assert.Equal(2, summary.NewLevel);
        Assert.Equal(100, summary.XpEarned);
    }

    [Fact]
    public async Task ProcessBattleRewards_LevelUp_Awards250GoldBonus()
    {
        using var context = TestDbContextFactory.Create();
        var player = TestDbContextFactory.CreatePlayer(context, "lvlgold", SubscriptionTier.Free, currency: 0, level: 1);
        player.LastFirstBattleBonus = DateTime.UtcNow;
        player.ExperiencePoints = 700;
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var summary = await service.ProcessBattleRewardsAsync(player.Id, won: true, ratingChange: 20);

        // Win base=50, streak=1*10=10, first battle=no => 60, multiplier 1.0 => 60 + 250 level-up bonus = 310
        Assert.Equal(310, summary.GoldEarned);

        // Verify notification was sent
        _notificationsMock.Verify(
            n => n.SendAsync(
                player.Id,
                NotificationType.LevelUp,
                It.IsAny<string>(),
                It.Is<string>(msg => msg.Contains("level 2")),
                It.IsAny<string?>()),
            Times.Once);
    }

    // ---------------------------------------------------------------
    // Guild gold bonus
    // ---------------------------------------------------------------

    [Fact]
    public async Task ProcessBattleRewards_GuildGoldBonus_AppliedWhenInGuild()
    {
        using var context = TestDbContextFactory.Create();
        var player = TestDbContextFactory.CreatePlayer(context, "guilded", SubscriptionTier.Free, currency: 0, level: 1);
        player.LastFirstBattleBonus = DateTime.UtcNow;
        await context.SaveChangesAsync();

        // Create a guild with 10% gold bonus
        var (guild, _) = TestDbContextFactory.CreateGuildWithLeader(context, player, "Gold Guild", "GG");
        guild.GoldBonusPercent = 10;
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var summary = await service.ProcessBattleRewardsAsync(player.Id, won: false, ratingChange: -5);

        // Loss base=10, no streak, no first battle => 10 * 1.0 = 10, + guild 10% bonus = 10 + 1 = 11
        Assert.Equal(11, summary.GoldEarned);
    }

    // ---------------------------------------------------------------
    // AwardCurrencyAsync
    // ---------------------------------------------------------------

    [Fact]
    public async Task AwardCurrency_FreeTier_ReturnsBaseGold()
    {
        using var context = TestDbContextFactory.Create();
        var player = TestDbContextFactory.CreatePlayer(context, "curr_free", SubscriptionTier.Free, currency: 0);
        var service = CreateService(context);

        int actual = await service.AwardCurrencyAsync(player.Id, 100);

        Assert.Equal(100, actual);
        await context.Entry(player).ReloadAsync();
        Assert.Equal(100, player.Currency);
    }

    [Fact]
    public async Task AwardCurrency_PremiumTier_AppliesMultiplier()
    {
        using var context = TestDbContextFactory.Create();
        var player = TestDbContextFactory.CreatePlayer(context, "curr_prem", SubscriptionTier.Premium, currency: 0);
        var service = CreateService(context);

        int actual = await service.AwardCurrencyAsync(player.Id, 100);

        // 100 * 1.5 = 150
        Assert.Equal(150, actual);
        await context.Entry(player).ReloadAsync();
        Assert.Equal(150, player.Currency);
    }

    [Fact]
    public async Task AwardCurrency_PremiumPlusTier_AppliesMultiplier()
    {
        using var context = TestDbContextFactory.Create();
        var player = TestDbContextFactory.CreatePlayer(context, "curr_pp", SubscriptionTier.PremiumPlus, currency: 0);
        var service = CreateService(context);

        int actual = await service.AwardCurrencyAsync(player.Id, 100);

        // 100 * 2.0 = 200
        Assert.Equal(200, actual);
        await context.Entry(player).ReloadAsync();
        Assert.Equal(200, player.Currency);
    }

    // ---------------------------------------------------------------
    // AwardExperienceAsync
    // ---------------------------------------------------------------

    [Fact]
    public async Task AwardExperience_BelowThreshold_ReturnsNull()
    {
        using var context = TestDbContextFactory.Create();
        var player = TestDbContextFactory.CreatePlayer(context, "xp_no_lvl", SubscriptionTier.Free, currency: 0, level: 1);
        var service = CreateService(context);

        int? newLevel = await service.AwardExperienceAsync(player.Id, 100);

        Assert.Null(newLevel);
        await context.Entry(player).ReloadAsync();
        Assert.Equal(1, player.Level);
        Assert.Equal(100, player.ExperiencePoints);
    }

    [Fact]
    public async Task AwardExperience_AtThreshold_TriggersLevelUp()
    {
        using var context = TestDbContextFactory.Create();
        var player = TestDbContextFactory.CreatePlayer(context, "xp_lvl", SubscriptionTier.Free, currency: 0, level: 1);
        player.ExperiencePoints = 700;
        await context.SaveChangesAsync();
        var service = CreateService(context);

        int? newLevel = await service.AwardExperienceAsync(player.Id, 100);

        Assert.Equal(2, newLevel);
        await context.Entry(player).ReloadAsync();
        Assert.Equal(2, player.Level);
        Assert.Equal(250, player.Currency); // 0 + 250 level-up bonus
        // XP should carry over: 700 + 100 - 750 = 50
        Assert.Equal(50, player.ExperiencePoints);

        _notificationsMock.Verify(
            n => n.SendAsync(
                player.Id,
                NotificationType.LevelUp,
                It.IsAny<string>(),
                It.Is<string>(msg => msg.Contains("level 2")),
                It.IsAny<string?>()),
            Times.Once);
    }

    // ---------------------------------------------------------------
    // GetXpForLevel formula validation
    // ---------------------------------------------------------------

    [Theory]
    [InlineData(1, 750)]     // 500 * 1 * 1.5 = 750
    [InlineData(2, 1500)]    // 500 * 2 * 1.5 = 1500
    [InlineData(5, 3750)]    // 500 * 5 * 1.5 = 3750
    [InlineData(10, 7500)]   // 500 * 10 * 1.5 = 7500
    [InlineData(50, 37500)]  // 500 * 50 * 1.5 = 37500
    public void GetXpForLevel_ReturnsCorrectFormula(int level, int expectedXp)
    {
        using var context = TestDbContextFactory.Create();
        var service = CreateService(context);

        int xp = service.GetXpForLevel(level);

        Assert.Equal(expectedXp, xp);
    }

    // ---------------------------------------------------------------
    // GetGoldMultiplier
    // ---------------------------------------------------------------

    [Theory]
    [InlineData(SubscriptionTier.Free, 1.0)]
    [InlineData(SubscriptionTier.Premium, 1.5)]
    [InlineData(SubscriptionTier.PremiumPlus, 2.0)]
    public void GetGoldMultiplier_ReturnsCorrectValue(SubscriptionTier tier, double expected)
    {
        using var context = TestDbContextFactory.Create();
        var service = CreateService(context);

        decimal multiplier = service.GetGoldMultiplier(tier);

        Assert.Equal((decimal)expected, multiplier);
    }

    // ---------------------------------------------------------------
    // Player not found
    // ---------------------------------------------------------------

    [Fact]
    public async Task ProcessBattleRewards_PlayerNotFound_ThrowsKeyNotFoundException()
    {
        using var context = TestDbContextFactory.Create();
        var service = CreateService(context);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.ProcessBattleRewardsAsync(Guid.NewGuid(), won: true, ratingChange: 10));
    }

    [Fact]
    public async Task AwardCurrency_PlayerNotFound_ThrowsKeyNotFoundException()
    {
        using var context = TestDbContextFactory.Create();
        var service = CreateService(context);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.AwardCurrencyAsync(Guid.NewGuid(), 100));
    }

    [Fact]
    public async Task AwardExperience_PlayerNotFound_ThrowsKeyNotFoundException()
    {
        using var context = TestDbContextFactory.Create();
        var service = CreateService(context);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.AwardExperienceAsync(Guid.NewGuid(), 100));
    }
}
