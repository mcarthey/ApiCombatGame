using ApiCombatGame.Data;
using ApiCombatGame.Models.Domain;
using ApiCombatGame.Models.Enums;
using ApiCombatGame.Services;
using ApiCombatGame.Tests.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ApiCombatGame.Tests.UnitTests;

/// <summary>
/// Unit tests for SubscriptionService. Tests DB operations directly
/// without hitting the Stripe API. Covers cancellation, updates,
/// tier determination, and subscription queries.
/// </summary>
public class SubscriptionServiceTests
{
    private readonly GameDbContext _context;
    private readonly SubscriptionService _service;
    private readonly Mock<IConfiguration> _config;

    public SubscriptionServiceTests()
    {
        _context = TestDbContextFactory.Create();
        _config = new Mock<IConfiguration>();
        _config.Setup(c => c["Stripe:PriceIds:Premium"]).Returns("price_premium_test");
        _config.Setup(c => c["Stripe:PriceIds:PremiumPlus"]).Returns("price_plus_test");
        var logger = new Mock<ILogger<SubscriptionService>>();
        _service = new SubscriptionService(_context, _config.Object, logger.Object);
    }

    // ── HandleSubscriptionCanceledAsync ──

    [Fact]
    public async Task CancelSubscription_ActiveSubscription_SetsStatusToCanceled()
    {
        var player = TestDbContextFactory.CreatePlayer(_context, "cancel_test", SubscriptionTier.Premium);
        var sub = CreateSubscription(player.Id, "sub_cancel_1", SubscriptionTier.Premium);

        await _service.HandleSubscriptionCanceledAsync("sub_cancel_1");

        var updated = await _context.Subscriptions.FindAsync(sub.Id);
        Assert.Equal(SubscriptionStatus.Canceled, updated!.Status);
        Assert.NotNull(updated.CanceledAt);
    }

    [Fact]
    public async Task CancelSubscription_DowngradesPlayerToFree()
    {
        var player = TestDbContextFactory.CreatePlayer(_context, "downgrade_test", SubscriptionTier.PremiumPlus);
        CreateSubscription(player.Id, "sub_downgrade_1", SubscriptionTier.PremiumPlus);

        await _service.HandleSubscriptionCanceledAsync("sub_downgrade_1");

        var updatedPlayer = await _context.Players.FindAsync(player.Id);
        Assert.Equal(SubscriptionTier.Free, updatedPlayer!.CurrentTier);
    }

    [Fact]
    public async Task CancelSubscription_UnknownSubscriptionId_DoesNotThrow()
    {
        // Should log warning but not throw
        await _service.HandleSubscriptionCanceledAsync("sub_nonexistent");
    }

    [Fact]
    public async Task CancelSubscription_PremiumPlayer_RevertedToFreePerks()
    {
        var player = TestDbContextFactory.CreatePlayer(_context, "perks_revert", SubscriptionTier.Premium);
        CreateSubscription(player.Id, "sub_perks_1", SubscriptionTier.Premium);

        await _service.HandleSubscriptionCanceledAsync("sub_perks_1");

        var updatedPlayer = await _context.Players.FindAsync(player.Id);
        Assert.Equal(SubscriptionTier.Free, updatedPlayer!.CurrentTier);
    }

    // ── HandleSubscriptionUpdatedAsync ──

    [Fact]
    public async Task UpdateSubscription_ActiveStatus_KeepsTier()
    {
        var player = TestDbContextFactory.CreatePlayer(_context, "update_active", SubscriptionTier.Premium);
        CreateSubscription(player.Id, "sub_update_1", SubscriptionTier.Premium, "price_premium_test");

        var newStart = DateTime.UtcNow;
        var newEnd = DateTime.UtcNow.AddMonths(1);

        await _service.HandleSubscriptionUpdatedAsync("sub_update_1", "price_premium_test", "active", newStart, newEnd);

        var updatedPlayer = await _context.Players.FindAsync(player.Id);
        Assert.Equal(SubscriptionTier.Premium, updatedPlayer!.CurrentTier);
    }

    [Fact]
    public async Task UpdateSubscription_PastDueStatus_DowngradesToFree()
    {
        var player = TestDbContextFactory.CreatePlayer(_context, "update_pastdue", SubscriptionTier.Premium);
        CreateSubscription(player.Id, "sub_pastdue_1", SubscriptionTier.Premium, "price_premium_test");

        await _service.HandleSubscriptionUpdatedAsync("sub_pastdue_1", "price_premium_test", "past_due",
            DateTime.UtcNow, DateTime.UtcNow.AddMonths(1));

        var updatedPlayer = await _context.Players.FindAsync(player.Id);
        Assert.Equal(SubscriptionTier.Free, updatedPlayer!.CurrentTier);
    }

    [Fact]
    public async Task UpdateSubscription_CanceledStatus_DowngradesToFree()
    {
        var player = TestDbContextFactory.CreatePlayer(_context, "update_canceled", SubscriptionTier.PremiumPlus);
        CreateSubscription(player.Id, "sub_canceled_1", SubscriptionTier.PremiumPlus, "price_plus_test");

        await _service.HandleSubscriptionUpdatedAsync("sub_canceled_1", "price_plus_test", "canceled",
            DateTime.UtcNow, DateTime.UtcNow.AddMonths(1));

        var updatedPlayer = await _context.Players.FindAsync(player.Id);
        Assert.Equal(SubscriptionTier.Free, updatedPlayer!.CurrentTier);

        var sub = _context.Subscriptions.First(s => s.StripeSubscriptionId == "sub_canceled_1");
        Assert.Equal(SubscriptionStatus.Canceled, sub.Status);
    }

    [Fact]
    public async Task UpdateSubscription_UpgradeFromPremiumToPlus_UpdatesTier()
    {
        var player = TestDbContextFactory.CreatePlayer(_context, "upgrade_test", SubscriptionTier.Premium);
        CreateSubscription(player.Id, "sub_upgrade_1", SubscriptionTier.Premium, "price_premium_test");

        await _service.HandleSubscriptionUpdatedAsync("sub_upgrade_1", "price_plus_test", "active",
            DateTime.UtcNow, DateTime.UtcNow.AddMonths(1));

        var updatedPlayer = await _context.Players.FindAsync(player.Id);
        Assert.Equal(SubscriptionTier.PremiumPlus, updatedPlayer!.CurrentTier);

        var sub = _context.Subscriptions.First(s => s.StripeSubscriptionId == "sub_upgrade_1");
        Assert.Equal(SubscriptionTier.PremiumPlus, sub.Tier);
    }

    [Fact]
    public async Task UpdateSubscription_DowngradeFromPlusToPremium_UpdatesTier()
    {
        var player = TestDbContextFactory.CreatePlayer(_context, "downgrade_tier", SubscriptionTier.PremiumPlus);
        CreateSubscription(player.Id, "sub_down_1", SubscriptionTier.PremiumPlus, "price_plus_test");

        await _service.HandleSubscriptionUpdatedAsync("sub_down_1", "price_premium_test", "active",
            DateTime.UtcNow, DateTime.UtcNow.AddMonths(1));

        var updatedPlayer = await _context.Players.FindAsync(player.Id);
        Assert.Equal(SubscriptionTier.Premium, updatedPlayer!.CurrentTier);
    }

    [Fact]
    public async Task UpdateSubscription_IncompleteStatus_DowngradesToFree()
    {
        var player = TestDbContextFactory.CreatePlayer(_context, "incomplete_test", SubscriptionTier.Premium);
        CreateSubscription(player.Id, "sub_incomplete_1", SubscriptionTier.Premium, "price_premium_test");

        await _service.HandleSubscriptionUpdatedAsync("sub_incomplete_1", "price_premium_test", "incomplete",
            DateTime.UtcNow, DateTime.UtcNow.AddMonths(1));

        var updatedPlayer = await _context.Players.FindAsync(player.Id);
        Assert.Equal(SubscriptionTier.Free, updatedPlayer!.CurrentTier);

        var sub = _context.Subscriptions.First(s => s.StripeSubscriptionId == "sub_incomplete_1");
        Assert.Equal(SubscriptionStatus.Incomplete, sub.Status);
    }

    [Fact]
    public async Task UpdateSubscription_UnknownId_DoesNotThrow()
    {
        await _service.HandleSubscriptionUpdatedAsync("sub_unknown", "price_premium_test", "active",
            DateTime.UtcNow, DateTime.UtcNow.AddMonths(1));
    }

    [Fact]
    public async Task UpdateSubscription_UpdatesBillingPeriod()
    {
        var player = TestDbContextFactory.CreatePlayer(_context, "period_test");
        CreateSubscription(player.Id, "sub_period_1", SubscriptionTier.Premium, "price_premium_test");

        var newStart = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var newEnd = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);

        await _service.HandleSubscriptionUpdatedAsync("sub_period_1", "price_premium_test", "active", newStart, newEnd);

        var sub = _context.Subscriptions.First(s => s.StripeSubscriptionId == "sub_period_1");
        Assert.Equal(newStart, sub.CurrentPeriodStart);
        Assert.Equal(newEnd, sub.CurrentPeriodEnd);
    }

    // ── GetSubscriptionAsync ──

    [Fact]
    public async Task GetSubscription_ExistingPlayer_ReturnsSubscription()
    {
        var player = TestDbContextFactory.CreatePlayer(_context, "get_test");
        CreateSubscription(player.Id, "sub_get_1", SubscriptionTier.Premium);

        var result = await _service.GetSubscriptionAsync(player.Id);

        Assert.NotNull(result);
        Assert.Equal("sub_get_1", result.StripeSubscriptionId);
        Assert.Equal(SubscriptionTier.Premium, result.Tier);
    }

    [Fact]
    public async Task GetSubscription_NoSubscription_ReturnsNull()
    {
        var player = TestDbContextFactory.CreatePlayer(_context, "no_sub");

        var result = await _service.GetSubscriptionAsync(player.Id);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetSubscription_UnknownPlayer_ReturnsNull()
    {
        var result = await _service.GetSubscriptionAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    // ── Tier determination via update ──

    [Fact]
    public async Task TierDetermination_UnknownPriceId_DefaultsToFree()
    {
        var player = TestDbContextFactory.CreatePlayer(_context, "unknown_price", SubscriptionTier.Premium);
        CreateSubscription(player.Id, "sub_unknown_price", SubscriptionTier.Premium, "price_premium_test");

        await _service.HandleSubscriptionUpdatedAsync("sub_unknown_price", "price_something_else", "active",
            DateTime.UtcNow, DateTime.UtcNow.AddMonths(1));

        var sub = _context.Subscriptions.First(s => s.StripeSubscriptionId == "sub_unknown_price");
        Assert.Equal(SubscriptionTier.Free, sub.Tier);
    }

    [Fact]
    public async Task TierDetermination_PremiumPriceId_SetsPremium()
    {
        var player = TestDbContextFactory.CreatePlayer(_context, "premium_price");
        CreateSubscription(player.Id, "sub_prem_price", SubscriptionTier.Free, "price_old");

        await _service.HandleSubscriptionUpdatedAsync("sub_prem_price", "price_premium_test", "active",
            DateTime.UtcNow, DateTime.UtcNow.AddMonths(1));

        var sub = _context.Subscriptions.First(s => s.StripeSubscriptionId == "sub_prem_price");
        Assert.Equal(SubscriptionTier.Premium, sub.Tier);
    }

    [Fact]
    public async Task TierDetermination_PlusPriceId_SetsPremiumPlus()
    {
        var player = TestDbContextFactory.CreatePlayer(_context, "plus_price");
        CreateSubscription(player.Id, "sub_plus_price", SubscriptionTier.Free, "price_old");

        await _service.HandleSubscriptionUpdatedAsync("sub_plus_price", "price_plus_test", "active",
            DateTime.UtcNow, DateTime.UtcNow.AddMonths(1));

        var sub = _context.Subscriptions.First(s => s.StripeSubscriptionId == "sub_plus_price");
        Assert.Equal(SubscriptionTier.PremiumPlus, sub.Tier);
    }

    // ── Helper ──

    private Subscription CreateSubscription(Guid playerId, string stripeSubId,
        SubscriptionTier tier, string priceId = "price_premium_test")
    {
        var sub = new Subscription
        {
            Id = Guid.NewGuid(),
            PlayerId = playerId,
            StripeCustomerId = $"cus_{Guid.NewGuid():N}",
            StripeSubscriptionId = stripeSubId,
            StripePriceId = priceId,
            Tier = tier,
            Status = SubscriptionStatus.Active,
            AmountUsd = tier == SubscriptionTier.PremiumPlus ? 9.99m : 4.99m,
            CurrentPeriodStart = DateTime.UtcNow,
            CurrentPeriodEnd = DateTime.UtcNow.AddMonths(1),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Subscriptions.Add(sub);
        _context.SaveChanges();
        return sub;
    }
}
