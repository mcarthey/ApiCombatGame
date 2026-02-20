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

public class GuildTreasuryServiceTests
{
    private static (GameDbContext context, GuildTreasuryService service) CreateService()
    {
        var context = TestDbContextFactory.Create();
        var service = new GuildTreasuryService(context, new Mock<IActivityLedger>().Object, new Mock<INotificationService>().Object, NullLogger<GuildTreasuryService>.Instance);
        return (context, service);
    }

    // ─── GetTreasuryAsync ───────────────────────────────────────────────

    [Fact]
    public async Task GetTreasury_ReturnsGuildAndUpgradesList()
    {
        var (context, service) = CreateService();
        var leader = TestDbContextFactory.CreatePlayer(context, "leader");
        var (guild, _) = TestDbContextFactory.CreateGuildWithLeader(context, leader);

        var (returnedGuild, upgrades) = await service.GetTreasuryAsync(guild.Id);

        Assert.Equal(guild.Id, returnedGuild.Id);
        Assert.NotNull(upgrades);
        Assert.Equal(6, upgrades.Count);
    }

    [Fact]
    public async Task GetTreasury_ThrowsForNonexistentGuild()
    {
        var (_, service) = CreateService();

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.GetTreasuryAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetTreasury_MarksAvailableUpgradesCorrectly()
    {
        var (context, service) = CreateService();
        var leader = TestDbContextFactory.CreatePlayer(context, "leader");
        var (guild, _) = TestDbContextFactory.CreateGuildWithLeader(context, leader);

        // Give the guild enough to afford max_members_30 (50000g)
        guild.TreasuryBalance = 50_000;
        context.SaveChanges();

        var (_, upgrades) = await service.GetTreasuryAsync(guild.Id);

        var maxMembers30 = upgrades.First(u => u.Id == "max_members_30");
        Assert.True(maxMembers30.CanAfford);
        Assert.False(maxMembers30.AlreadyPurchased);
    }

    // ─── PurchaseUpgradeAsync ───────────────────────────────────────────

    [Fact]
    public async Task PurchaseUpgrade_SucceedsForLeaderWithEnoughFunds()
    {
        var (context, service) = CreateService();
        var leader = TestDbContextFactory.CreatePlayer(context, "leader");
        var (guild, _) = TestDbContextFactory.CreateGuildWithLeader(context, leader);
        guild.TreasuryBalance = 100_000;
        context.SaveChanges();

        var result = await service.PurchaseUpgradeAsync(guild.Id, leader.Id, "max_members_30");

        Assert.NotNull(result);
    }

    [Fact]
    public async Task PurchaseUpgrade_DeductsTreasuryBalance()
    {
        var (context, service) = CreateService();
        var leader = TestDbContextFactory.CreatePlayer(context, "leader");
        var (guild, _) = TestDbContextFactory.CreateGuildWithLeader(context, leader);
        guild.TreasuryBalance = 100_000;
        context.SaveChanges();

        var result = await service.PurchaseUpgradeAsync(guild.Id, leader.Id, "max_members_30");

        Assert.Equal(50_000, result.TreasuryBalance); // 100k - 50k cost
    }

    [Fact]
    public async Task PurchaseUpgrade_AppliesMaxMembersEffect()
    {
        var (context, service) = CreateService();
        var leader = TestDbContextFactory.CreatePlayer(context, "leader");
        var (guild, _) = TestDbContextFactory.CreateGuildWithLeader(context, leader);
        guild.TreasuryBalance = 60_000;
        context.SaveChanges();

        Assert.Equal(20, guild.MaxMembers); // default

        var result = await service.PurchaseUpgradeAsync(guild.Id, leader.Id, "max_members_30");

        Assert.Equal(30, result.MaxMembers);
    }

    [Fact]
    public async Task PurchaseUpgrade_AppliesGoldBonusEffect()
    {
        var (context, service) = CreateService();
        var leader = TestDbContextFactory.CreatePlayer(context, "leader");
        var (guild, _) = TestDbContextFactory.CreateGuildWithLeader(context, leader);
        guild.TreasuryBalance = 50_000;
        context.SaveChanges();

        Assert.Equal(0, guild.GoldBonusPercent);

        var result = await service.PurchaseUpgradeAsync(guild.Id, leader.Id, "gold_bonus_10");

        Assert.Equal(10, result.GoldBonusPercent);
    }

    [Fact]
    public async Task PurchaseUpgrade_AppliesRaidAttemptsEffect()
    {
        var (context, service) = CreateService();
        var leader = TestDbContextFactory.CreatePlayer(context, "leader");
        var (guild, _) = TestDbContextFactory.CreateGuildWithLeader(context, leader);
        guild.TreasuryBalance = 50_000;
        context.SaveChanges();

        Assert.Equal(3, guild.MaxRaidAttempts);

        var result = await service.PurchaseUpgradeAsync(guild.Id, leader.Id, "raid_attempts_4");

        Assert.Equal(4, result.MaxRaidAttempts);
    }

    [Fact]
    public async Task PurchaseUpgrade_FailsForNonLeader()
    {
        var (context, service) = CreateService();
        var leader = TestDbContextFactory.CreatePlayer(context, "leader");
        var member = TestDbContextFactory.CreatePlayer(context, "member");
        var (guild, _) = TestDbContextFactory.CreateGuildWithLeader(context, leader);
        TestDbContextFactory.AddGuildMember(context, guild.Id, member);
        guild.TreasuryBalance = 100_000;
        context.SaveChanges();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.PurchaseUpgradeAsync(guild.Id, member.Id, "max_members_30"));

        Assert.Contains("leader", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PurchaseUpgrade_FailsWithInsufficientTreasury()
    {
        var (context, service) = CreateService();
        var leader = TestDbContextFactory.CreatePlayer(context, "leader");
        var (guild, _) = TestDbContextFactory.CreateGuildWithLeader(context, leader);
        guild.TreasuryBalance = 100; // not enough for any upgrade
        context.SaveChanges();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.PurchaseUpgradeAsync(guild.Id, leader.Id, "max_members_30"));

        Assert.Contains("Insufficient", ex.Message);
    }

    [Fact]
    public async Task PurchaseUpgrade_FailsForAlreadyPurchasedUpgrade()
    {
        var (context, service) = CreateService();
        var leader = TestDbContextFactory.CreatePlayer(context, "leader");
        var (guild, _) = TestDbContextFactory.CreateGuildWithLeader(context, leader);
        guild.TreasuryBalance = 200_000;
        context.SaveChanges();

        // Purchase once
        await service.PurchaseUpgradeAsync(guild.Id, leader.Id, "max_members_30");

        // Attempt to purchase the same upgrade again
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.PurchaseUpgradeAsync(guild.Id, leader.Id, "max_members_30"));

        Assert.Contains("not available", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PurchaseUpgrade_FailsForUnknownUpgradeId()
    {
        var (context, service) = CreateService();
        var leader = TestDbContextFactory.CreatePlayer(context, "leader");
        var (guild, _) = TestDbContextFactory.CreateGuildWithLeader(context, leader);
        guild.TreasuryBalance = 200_000;
        context.SaveChanges();

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.PurchaseUpgradeAsync(guild.Id, leader.Id, "nonexistent_upgrade"));
    }

    // ─── DepositAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task Deposit_TransfersCurrencyFromPlayerToTreasury()
    {
        var (context, service) = CreateService();
        var leader = TestDbContextFactory.CreatePlayer(context, "leader", currency: 10_000);
        var (guild, _) = TestDbContextFactory.CreateGuildWithLeader(context, leader);

        Assert.Equal(0, guild.TreasuryBalance);

        var result = await service.DepositAsync(guild.Id, leader.Id, 5_000);

        Assert.Equal(5_000, result.TreasuryBalance);
        Assert.Equal(5_000, leader.Currency);
    }

    [Fact]
    public async Task Deposit_FailsIfPlayerHasInsufficientCurrency()
    {
        var (context, service) = CreateService();
        var leader = TestDbContextFactory.CreatePlayer(context, "leader", currency: 100);
        var (guild, _) = TestDbContextFactory.CreateGuildWithLeader(context, leader);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.DepositAsync(guild.Id, leader.Id, 5_000));

        Assert.Contains("Insufficient", ex.Message);
    }

    [Fact]
    public async Task Deposit_FailsForNonMember()
    {
        var (context, service) = CreateService();
        var leader = TestDbContextFactory.CreatePlayer(context, "leader", currency: 10_000);
        var outsider = TestDbContextFactory.CreatePlayer(context, "outsider", currency: 10_000);
        var (guild, _) = TestDbContextFactory.CreateGuildWithLeader(context, leader);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.DepositAsync(guild.Id, outsider.Id, 1_000));

        Assert.Contains("not a member", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Deposit_UpdatesContributionPoints()
    {
        var (context, service) = CreateService();
        var leader = TestDbContextFactory.CreatePlayer(context, "leader", currency: 50_000);
        var (guild, leaderMembership) = TestDbContextFactory.CreateGuildWithLeader(context, leader);

        Assert.Equal(0, leaderMembership.ContributionPoints);

        await service.DepositAsync(guild.Id, leader.Id, 3_000);

        Assert.Equal(3_000, leaderMembership.ContributionPoints);
    }

    [Fact]
    public async Task Deposit_AccumulatesContributionPointsOverMultipleDeposits()
    {
        var (context, service) = CreateService();
        var leader = TestDbContextFactory.CreatePlayer(context, "leader", currency: 50_000);
        var (guild, leaderMembership) = TestDbContextFactory.CreateGuildWithLeader(context, leader);

        await service.DepositAsync(guild.Id, leader.Id, 2_000);
        await service.DepositAsync(guild.Id, leader.Id, 3_000);

        Assert.Equal(5_000, leaderMembership.ContributionPoints);
        Assert.Equal(5_000, guild.TreasuryBalance);
        Assert.Equal(45_000, leader.Currency);
    }

    [Fact]
    public async Task Deposit_FailsForZeroAmount()
    {
        var (context, service) = CreateService();
        var leader = TestDbContextFactory.CreatePlayer(context, "leader", currency: 10_000);
        var (guild, _) = TestDbContextFactory.CreateGuildWithLeader(context, leader);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.DepositAsync(guild.Id, leader.Id, 0));
    }

    [Fact]
    public async Task Deposit_FailsForNegativeAmount()
    {
        var (context, service) = CreateService();
        var leader = TestDbContextFactory.CreatePlayer(context, "leader", currency: 10_000);
        var (guild, _) = TestDbContextFactory.CreateGuildWithLeader(context, leader);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.DepositAsync(guild.Id, leader.Id, -500));
    }

    [Fact]
    public async Task Deposit_RegularMemberCanDeposit()
    {
        var (context, service) = CreateService();
        var leader = TestDbContextFactory.CreatePlayer(context, "leader", currency: 10_000);
        var member = TestDbContextFactory.CreatePlayer(context, "member", currency: 10_000);
        var (guild, _) = TestDbContextFactory.CreateGuildWithLeader(context, leader);
        var membership = TestDbContextFactory.AddGuildMember(context, guild.Id, member);

        await service.DepositAsync(guild.Id, member.Id, 2_000);

        Assert.Equal(2_000, guild.TreasuryBalance);
        Assert.Equal(8_000, member.Currency);
        Assert.Equal(2_000, membership.ContributionPoints);
    }
}
