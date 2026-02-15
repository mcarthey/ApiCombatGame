using ApiCombatGame.Models.Domain;
using ApiCombatGame.Models.Enums;
using ApiCombatGame.Services;
using ApiCombatGame.Tests.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Stripe;
using Xunit;

namespace ApiCombatGame.Tests.IntegrationTests;

/// <summary>
/// Integration tests that hit the real Stripe sandbox API.
/// These tests create actual Stripe test-mode customers, payment methods,
/// and subscriptions to verify the full payment flow works end-to-end.
///
/// Requires: Stripe test API key in appsettings.Development.json
/// Uses: Test card token "pm_card_visa" (equivalent of 4242 4242 4242 4242)
///
/// These tests are slower than unit tests (~2-5s each) because they make
/// real HTTP calls to Stripe's API.
/// </summary>
public class StripeSandboxTests : IDisposable
{
    private readonly Data.GameDbContext _context;
    private readonly ApiCombatGame.Services.SubscriptionService _service;
    private readonly string _premiumPriceId;
    private readonly string _plusPriceId;
    private readonly List<string> _createdCustomerIds = new();
    private readonly List<string> _createdSubscriptionIds = new();
    private readonly bool _stripeConfigured;

    public StripeSandboxTests()
    {
        _context = TestDbContextFactory.Create();

        // Load real config from appsettings
        var config = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "ApiCombatGame"))
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var stripeKey = config["Stripe:SecretKey"];
        _premiumPriceId = config["Stripe:PriceIds:Premium"] ?? "price_premium_test";
        _plusPriceId = config["Stripe:PriceIds:PremiumPlus"] ?? "price_plus_test";

        // Only configure Stripe if we have a real key (not placeholder)
        _stripeConfigured = !string.IsNullOrEmpty(stripeKey)
            && stripeKey.StartsWith("sk_test_")
            && stripeKey != "sk_test_your_test_key";

        if (_stripeConfigured)
        {
            StripeConfiguration.ApiKey = stripeKey;
        }

        var logger = new Mock<ILogger<ApiCombatGame.Services.SubscriptionService>>();
        _service = new ApiCombatGame.Services.SubscriptionService(_context, config, logger.Object);
    }

    public void Dispose()
    {
        // Clean up Stripe test data
        if (_stripeConfigured)
        {
            foreach (var subId in _createdSubscriptionIds)
            {
                try
                {
                    var service = new Stripe.SubscriptionService();
                    service.Cancel(subId);
                }
                catch { /* best-effort cleanup */ }
            }
            foreach (var cusId in _createdCustomerIds)
            {
                try
                {
                    var service = new CustomerService();
                    service.Delete(cusId);
                }
                catch { /* best-effort cleanup */ }
            }
        }
        _context.Dispose();
    }

    // ── Checkout Session Creation ──

    [Fact]
    public async Task CreateCheckoutSession_Premium_ReturnsStripeUrl()
    {
        if (!_stripeConfigured) return;

        var player = TestDbContextFactory.CreatePlayer(_context, "checkout_premium");
        var baseUrl = "https://apicombat.com";

        var url = await _service.CreateCheckoutSessionAsync(player.Id, "premium", baseUrl);

        Assert.NotNull(url);
        Assert.StartsWith("https://checkout.stripe.com/", url);
    }

    [Fact]
    public async Task CreateCheckoutSession_PremiumPlus_ReturnsStripeUrl()
    {
        if (!_stripeConfigured) return;

        var player = TestDbContextFactory.CreatePlayer(_context, "checkout_plus");
        var baseUrl = "https://apicombat.com";

        var url = await _service.CreateCheckoutSessionAsync(player.Id, "premium_plus", baseUrl);

        Assert.NotNull(url);
        Assert.StartsWith("https://checkout.stripe.com/", url);
    }

    [Fact]
    public async Task CreateCheckoutSession_InvalidTier_ThrowsException()
    {
        if (!_stripeConfigured) return;

        var player = TestDbContextFactory.CreatePlayer(_context, "checkout_invalid");
        var baseUrl = "https://apicombat.com";

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateCheckoutSessionAsync(player.Id, "diamond", baseUrl));
    }

    [Fact]
    public async Task CreateCheckoutSession_NonExistentPlayer_ThrowsException()
    {
        if (!_stripeConfigured) return;

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateCheckoutSessionAsync(Guid.NewGuid(), "premium", "https://apicombat.com"));
    }

    [Fact]
    public async Task CreateCheckoutSession_ExistingCustomer_ReusesCustomerId()
    {
        if (!_stripeConfigured) return;

        var player = TestDbContextFactory.CreatePlayer(_context, "checkout_reuse");
        var customerId = await CreateStripeCustomer(player.Email);

        // Create existing subscription record with Stripe customer ID
        _context.Subscriptions.Add(new Models.Domain.Subscription
        {
            Id = Guid.NewGuid(),
            PlayerId = player.Id,
            StripeCustomerId = customerId,
            StripeSubscriptionId = "sub_old",
            Tier = SubscriptionTier.Free,
            Status = SubscriptionStatus.Canceled,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        _context.SaveChanges();

        var url = await _service.CreateCheckoutSessionAsync(player.Id, "premium", "https://apicombat.com");

        Assert.NotNull(url);
        Assert.StartsWith("https://checkout.stripe.com/", url);
    }

    // ── Full Subscription Lifecycle ──

    [Fact]
    public async Task FullLifecycle_CreateSubscription_PlayerTierUpdated()
    {
        if (!_stripeConfigured) return;

        var player = TestDbContextFactory.CreatePlayer(_context, "lifecycle_create");
        Assert.Equal(SubscriptionTier.Free, player.CurrentTier);

        // Create customer and subscription via Stripe API
        var customerId = await CreateStripeCustomer(player.Email);
        var subscription = await CreateStripeSubscription(customerId, _premiumPriceId, player.Id);

        // Process the subscription through our service
        await _service.HandleSubscriptionCreatedAsync(
            subscription.Id,
            customerId,
            subscription.Items.Data[0].Price.Id,
            subscription.CurrentPeriodStart,
            subscription.CurrentPeriodEnd);

        // Verify player tier updated
        var updatedPlayer = await _context.Players.FindAsync(player.Id);
        Assert.Equal(SubscriptionTier.Premium, updatedPlayer!.CurrentTier);

        // Verify subscription record created
        var sub = await _service.GetSubscriptionAsync(player.Id);
        Assert.NotNull(sub);
        Assert.Equal(SubscriptionStatus.Active, sub.Status);
        Assert.Equal(customerId, sub.StripeCustomerId);
        Assert.Equal(subscription.Id, sub.StripeSubscriptionId);
        Assert.True(sub.AmountUsd > 0);
    }

    [Fact]
    public async Task FullLifecycle_CreatePremiumPlus_PlayerGetsPlusPerks()
    {
        if (!_stripeConfigured) return;

        var player = TestDbContextFactory.CreatePlayer(_context, "lifecycle_plus");

        var customerId = await CreateStripeCustomer(player.Email);
        var subscription = await CreateStripeSubscription(customerId, _plusPriceId, player.Id);

        await _service.HandleSubscriptionCreatedAsync(
            subscription.Id,
            customerId,
            subscription.Items.Data[0].Price.Id,
            subscription.CurrentPeriodStart,
            subscription.CurrentPeriodEnd);

        var updatedPlayer = await _context.Players.FindAsync(player.Id);
        Assert.Equal(SubscriptionTier.PremiumPlus, updatedPlayer!.CurrentTier);
    }

    [Fact]
    public async Task FullLifecycle_CancelSubscription_PlayerDowngraded()
    {
        if (!_stripeConfigured) return;

        var player = TestDbContextFactory.CreatePlayer(_context, "lifecycle_cancel");
        var customerId = await CreateStripeCustomer(player.Email);
        var subscription = await CreateStripeSubscription(customerId, _premiumPriceId, player.Id);

        // First create the subscription
        await _service.HandleSubscriptionCreatedAsync(
            subscription.Id, customerId,
            subscription.Items.Data[0].Price.Id,
            subscription.CurrentPeriodStart,
            subscription.CurrentPeriodEnd);

        Assert.Equal(SubscriptionTier.Premium,
            (await _context.Players.FindAsync(player.Id))!.CurrentTier);

        // Now cancel via our service (calls real Stripe API)
        await _service.CancelSubscriptionAsync(player.Id);

        var sub = await _service.GetSubscriptionAsync(player.Id);
        Assert.NotNull(sub!.CancelAt);

        // Simulate Stripe sending the deleted webhook
        await _service.HandleSubscriptionCanceledAsync(subscription.Id);

        var finalPlayer = await _context.Players.FindAsync(player.Id);
        Assert.Equal(SubscriptionTier.Free, finalPlayer!.CurrentTier);
    }

    [Fact]
    public async Task FullLifecycle_UpgradeFromPremiumToPlus()
    {
        if (!_stripeConfigured) return;

        var player = TestDbContextFactory.CreatePlayer(_context, "lifecycle_upgrade");
        var customerId = await CreateStripeCustomer(player.Email);
        var subscription = await CreateStripeSubscription(customerId, _premiumPriceId, player.Id);

        // Start with Premium
        await _service.HandleSubscriptionCreatedAsync(
            subscription.Id, customerId,
            subscription.Items.Data[0].Price.Id,
            subscription.CurrentPeriodStart,
            subscription.CurrentPeriodEnd);

        Assert.Equal(SubscriptionTier.Premium,
            (await _context.Players.FindAsync(player.Id))!.CurrentTier);

        // Simulate Stripe updating the subscription to PremiumPlus
        await _service.HandleSubscriptionUpdatedAsync(
            subscription.Id, _plusPriceId, "active",
            DateTime.UtcNow, DateTime.UtcNow.AddMonths(1));

        Assert.Equal(SubscriptionTier.PremiumPlus,
            (await _context.Players.FindAsync(player.Id))!.CurrentTier);
    }

    // ── Declined Cards ──

    [Fact]
    public async Task DeclinedCard_SubscriptionCreationFails()
    {
        if (!_stripeConfigured) return;

        var customerId = await CreateStripeCustomer("declined@test.com");

        // Using a declined card token should throw StripeException at some point
        // in the attach → set default → create subscription flow
        await Assert.ThrowsAsync<StripeException>(async () =>
        {
            var pmService = new PaymentMethodService();
            var pm = await pmService.AttachAsync("pm_card_chargeDeclined", new PaymentMethodAttachOptions
            {
                Customer = customerId
            });

            await new CustomerService().UpdateAsync(customerId, new CustomerUpdateOptions
            {
                InvoiceSettings = new CustomerInvoiceSettingsOptions
                {
                    DefaultPaymentMethod = pm.Id
                }
            });

            var subService = new Stripe.SubscriptionService();
            await subService.CreateAsync(new SubscriptionCreateOptions
            {
                Customer = customerId,
                Items = new List<SubscriptionItemOptions>
                {
                    new() { Price = _premiumPriceId }
                },
                PaymentBehavior = "error_if_incomplete"
            });
        });
    }

    [Fact]
    public async Task DeclinedCard_PlayerStaysFree()
    {
        if (!_stripeConfigured) return;

        var player = TestDbContextFactory.CreatePlayer(_context, "declined_stays_free");
        Assert.Equal(SubscriptionTier.Free, player.CurrentTier);

        // Even if we simulate an incomplete status update, player stays Free
        // (simulating what happens when initial payment fails)
        _context.Subscriptions.Add(new Models.Domain.Subscription
        {
            Id = Guid.NewGuid(),
            PlayerId = player.Id,
            StripeCustomerId = "cus_fake",
            StripeSubscriptionId = "sub_declined",
            StripePriceId = _premiumPriceId,
            Tier = SubscriptionTier.Premium,
            Status = SubscriptionStatus.Incomplete,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        _context.SaveChanges();

        // Stripe sends updated event with incomplete status
        await _service.HandleSubscriptionUpdatedAsync(
            "sub_declined", _premiumPriceId, "incomplete",
            DateTime.UtcNow, DateTime.UtcNow.AddMonths(1));

        var updatedPlayer = await _context.Players.FindAsync(player.Id);
        Assert.Equal(SubscriptionTier.Free, updatedPlayer!.CurrentTier);
    }

    // ── Customer Portal ──

    [Fact]
    public async Task CustomerPortal_ActiveSubscription_ReturnsPortalUrl()
    {
        if (!_stripeConfigured) return;

        var player = TestDbContextFactory.CreatePlayer(_context, "portal_test");
        var customerId = await CreateStripeCustomer(player.Email);

        _context.Subscriptions.Add(new Models.Domain.Subscription
        {
            Id = Guid.NewGuid(),
            PlayerId = player.Id,
            StripeCustomerId = customerId,
            StripeSubscriptionId = "sub_portal",
            Tier = SubscriptionTier.Premium,
            Status = SubscriptionStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        _context.SaveChanges();

        var url = await _service.CreateCustomerPortalSessionAsync(
            player.Id, "https://apicombat.com/Account/Subscription");

        Assert.NotNull(url);
        Assert.StartsWith("https://billing.stripe.com/", url);
    }

    [Fact]
    public async Task CustomerPortal_NoSubscription_ThrowsException()
    {
        if (!_stripeConfigured) return;

        var player = TestDbContextFactory.CreatePlayer(_context, "portal_none");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateCustomerPortalSessionAsync(
                player.Id, "https://apicombat.com/Account/Subscription"));
    }

    // ── Cancel Without Stripe (no active sub) ──

    [Fact]
    public async Task CancelSubscription_NoActiveSub_ThrowsException()
    {
        if (!_stripeConfigured) return;

        var player = TestDbContextFactory.CreatePlayer(_context, "cancel_none");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CancelSubscriptionAsync(player.Id));
    }

    // ── Helpers ──

    private async Task<string> CreateStripeCustomer(string email)
    {
        var service = new CustomerService();
        var customer = await service.CreateAsync(new CustomerCreateOptions
        {
            Email = email,
            Name = $"Test Player ({email})",
            Metadata = new Dictionary<string, string>
            {
                ["source"] = "integration_test"
            }
        });
        _createdCustomerIds.Add(customer.Id);
        return customer.Id;
    }

    private async Task<Stripe.Subscription> CreateStripeSubscription(
        string customerId, string priceId, Guid playerId)
    {
        // Attach test payment method
        var pmService = new PaymentMethodService();
        var pm = await pmService.AttachAsync("pm_card_visa", new PaymentMethodAttachOptions
        {
            Customer = customerId
        });

        // Set as default payment method
        var cusService = new CustomerService();
        await cusService.UpdateAsync(customerId, new CustomerUpdateOptions
        {
            InvoiceSettings = new CustomerInvoiceSettingsOptions
            {
                DefaultPaymentMethod = pm.Id
            }
        });

        // Create subscription
        var subService = new Stripe.SubscriptionService();
        var subscription = await subService.CreateAsync(new SubscriptionCreateOptions
        {
            Customer = customerId,
            Items = new List<SubscriptionItemOptions>
            {
                new() { Price = priceId }
            },
            Metadata = new Dictionary<string, string>
            {
                ["playerId"] = playerId.ToString(),
                ["source"] = "integration_test"
            }
        });

        _createdSubscriptionIds.Add(subscription.Id);
        return subscription;
    }
}
