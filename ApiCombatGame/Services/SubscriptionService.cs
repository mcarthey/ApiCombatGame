using ApiCombatGame.Data;
using ApiCombatGame.Models.Domain;
using ApiCombatGame.Models.Enums;
using ApiCombatGame.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;

namespace ApiCombatGame.Services;

public class SubscriptionService : ISubscriptionService
{
    private readonly GameDbContext _context;
    private readonly IConfiguration _config;
    private readonly ILogger<SubscriptionService> _logger;

    public SubscriptionService(GameDbContext context, IConfiguration config, ILogger<SubscriptionService> logger)
    {
        _context = context;
        _config = config;
        _logger = logger;
    }

    public async Task<string> CreateCheckoutSessionAsync(Guid playerId, string tier, string baseUrl)
    {
        var player = await _context.Players.FindAsync(playerId)
            ?? throw new InvalidOperationException("Player not found.");

        var priceId = tier.ToLower() switch
        {
            "premium" => _config["Stripe:PriceIds:Premium"],
            "premium_plus" or "premiumplus" => _config["Stripe:PriceIds:PremiumPlus"],
            _ => throw new InvalidOperationException($"Unknown tier: {tier}")
        };

        if (string.IsNullOrEmpty(priceId))
            throw new InvalidOperationException($"Price ID not configured for tier: {tier}");

        // Check if player already has a Stripe customer ID
        var existingSub = await _context.Subscriptions.FirstOrDefaultAsync(s => s.PlayerId == playerId);
        string? customerId = existingSub?.StripeCustomerId;

        var options = new SessionCreateOptions
        {
            PaymentMethodTypes = new List<string> { "card" },
            LineItems = new List<SessionLineItemOptions>
            {
                new()
                {
                    Price = priceId,
                    Quantity = 1,
                },
            },
            Mode = "subscription",
            SuccessUrl = $"{baseUrl}/Account/Subscription?success=true",
            CancelUrl = $"{baseUrl}/Account/Subscription?canceled=true",
            ClientReferenceId = playerId.ToString(),
            Metadata = new Dictionary<string, string>
            {
                ["playerId"] = playerId.ToString(),
                ["tier"] = tier
            }
        };

        if (!string.IsNullOrEmpty(customerId))
        {
            options.Customer = customerId;
        }
        else
        {
            options.CustomerEmail = player.Email;
        }

        var service = new SessionService();
        var session = await service.CreateAsync(options);

        _logger.LogInformation("Created Stripe Checkout session {SessionId} for player {PlayerId}, tier: {Tier}",
            session.Id, playerId, tier);

        return session.Url!;
    }

    public async Task ChangeTierAsync(Guid playerId, string newTier)
    {
        var subscription = await _context.Subscriptions
            .FirstOrDefaultAsync(s => s.PlayerId == playerId && s.Status == SubscriptionStatus.Active);

        if (subscription == null || string.IsNullOrEmpty(subscription.StripeSubscriptionId))
            throw new InvalidOperationException("No active Stripe subscription found.");

        var newPriceId = newTier.ToLower() switch
        {
            "premium" => _config["Stripe:PriceIds:Premium"],
            "premium_plus" or "premiumplus" => _config["Stripe:PriceIds:PremiumPlus"],
            _ => throw new InvalidOperationException($"Unknown tier: {newTier}")
        };

        if (string.IsNullOrEmpty(newPriceId))
            throw new InvalidOperationException($"Price ID not configured for tier: {newTier}");

        // Fetch current subscription to get the subscription item ID
        var stripeService = new Stripe.SubscriptionService();
        var stripeSub = await stripeService.GetAsync(subscription.StripeSubscriptionId);
        var currentItem = stripeSub.Items.Data.FirstOrDefault()
            ?? throw new InvalidOperationException("Subscription has no items.");

        // Update the subscription: swap the price, enable proration, remove any pending cancellation
        var updatedSub = await stripeService.UpdateAsync(subscription.StripeSubscriptionId, new SubscriptionUpdateOptions
        {
            Items = new List<SubscriptionItemOptions>
            {
                new()
                {
                    Id = currentItem.Id,
                    Price = newPriceId,
                }
            },
            ProrationBehavior = "create_prorations",
            CancelAtPeriodEnd = false,
        });

        // Update local DB with authoritative Stripe data
        var minValid = new DateTime(2020, 1, 1);
        var tier = DetermineTierFromPriceId(newPriceId);
        var amount = updatedSub.Items.Data.FirstOrDefault()?.Price?.UnitAmount ?? 0;

        subscription.StripePriceId = newPriceId;
        subscription.Tier = tier;
        subscription.Status = SubscriptionStatus.Active;
        subscription.AmountUsd = amount / 100m;
        subscription.CancelAt = null;
        subscription.CanceledAt = null;
        subscription.UpdatedAt = DateTime.UtcNow;

        if (updatedSub.CurrentPeriodStart > minValid)
            subscription.CurrentPeriodStart = updatedSub.CurrentPeriodStart;
        if (updatedSub.CurrentPeriodEnd > minValid)
            subscription.CurrentPeriodEnd = updatedSub.CurrentPeriodEnd;

        var player = await _context.Players.FindAsync(playerId);
        if (player != null)
        {
            player.CurrentTier = tier;
            player.DailyBattlesUsed = 0;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Tier changed for player {PlayerId}: {Tier} (prorated)", playerId, tier);
    }

    public async Task HandleSubscriptionCreatedAsync(string stripeSubscriptionId, string customerId, string priceId, DateTime periodStart, DateTime periodEnd)
    {
        // Look up the Stripe subscription to get metadata
        var stripeService = new Stripe.SubscriptionService();
        var stripeSub = await stripeService.GetAsync(stripeSubscriptionId);

        Guid? playerId = null;
        if (stripeSub.Metadata.TryGetValue("playerId", out var playerIdStr))
        {
            playerId = Guid.Parse(playerIdStr);
        }

        // Also try to find by customer ID from existing subscription records
        if (!playerId.HasValue)
        {
            var existing = await _context.Subscriptions.FirstOrDefaultAsync(s => s.StripeCustomerId == customerId);
            playerId = existing?.PlayerId;
        }

        // Try to find by customer email
        if (!playerId.HasValue)
        {
            var customerService = new CustomerService();
            var customer = await customerService.GetAsync(customerId);
            if (customer?.Email != null)
            {
                var player = await _context.Players.FirstOrDefaultAsync(p => p.Email == customer.Email);
                playerId = player?.Id;
            }
        }

        if (!playerId.HasValue)
        {
            _logger.LogWarning("Could not resolve player for Stripe subscription {SubscriptionId}", stripeSubscriptionId);
            return;
        }

        var tier = DetermineTierFromPriceId(priceId);
        var amount = stripeSub.Items.Data.FirstOrDefault()?.Price?.UnitAmount ?? 0;

        var subscription = await _context.Subscriptions.FirstOrDefaultAsync(s => s.PlayerId == playerId.Value);

        if (subscription == null)
        {
            subscription = new Models.Domain.Subscription
            {
                Id = Guid.NewGuid(),
                PlayerId = playerId.Value,
                CreatedAt = DateTime.UtcNow
            };
            _context.Subscriptions.Add(subscription);
        }

        subscription.StripeCustomerId = customerId;
        subscription.StripeSubscriptionId = stripeSubscriptionId;
        subscription.StripePriceId = priceId;
        subscription.Tier = tier;
        subscription.Status = SubscriptionStatus.Active;
        subscription.AmountUsd = amount / 100m;
        subscription.CanceledAt = null;
        subscription.CancelAt = null;
        subscription.UpdatedAt = DateTime.UtcNow;

        // Use Stripe API response dates (authoritative) over webhook-deserialized params
        // which may be epoch zero due to API version mismatch
        var minValid = new DateTime(2020, 1, 1);
        var bestPeriodStart = stripeSub.CurrentPeriodStart > minValid ? stripeSub.CurrentPeriodStart : periodStart;
        var bestPeriodEnd = stripeSub.CurrentPeriodEnd > minValid ? stripeSub.CurrentPeriodEnd : periodEnd;
        if (bestPeriodStart > minValid)
            subscription.CurrentPeriodStart = bestPeriodStart;
        if (bestPeriodEnd > minValid)
            subscription.CurrentPeriodEnd = bestPeriodEnd;

        // Update player tier
        var playerEntity = await _context.Players.FindAsync(playerId.Value);
        if (playerEntity != null)
        {
            playerEntity.CurrentTier = tier;
            playerEntity.DailyBattlesUsed = 0;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Subscription created/updated for player {PlayerId}: {Tier}", playerId.Value, tier);
    }

    public async Task HandleSubscriptionCanceledAsync(string stripeSubscriptionId)
    {
        var subscription = await _context.Subscriptions
            .FirstOrDefaultAsync(s => s.StripeSubscriptionId == stripeSubscriptionId);

        if (subscription == null)
        {
            _logger.LogWarning("Subscription not found for cancellation: {SubscriptionId}", stripeSubscriptionId);
            return;
        }

        subscription.Status = SubscriptionStatus.Canceled;
        subscription.CanceledAt = DateTime.UtcNow;
        subscription.UpdatedAt = DateTime.UtcNow;

        var player = await _context.Players.FindAsync(subscription.PlayerId);
        if (player != null)
        {
            player.CurrentTier = SubscriptionTier.Free;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Subscription canceled for player {PlayerId}", subscription.PlayerId);
    }

    public async Task HandleSubscriptionUpdatedAsync(string stripeSubscriptionId, string priceId, string status, DateTime periodStart, DateTime periodEnd, DateTime? cancelAt = null)
    {
        var subscription = await _context.Subscriptions
            .FirstOrDefaultAsync(s => s.StripeSubscriptionId == stripeSubscriptionId);

        if (subscription == null) return;

        subscription.StripePriceId = priceId;
        subscription.Tier = DetermineTierFromPriceId(priceId);
        subscription.Status = status switch
        {
            "active" => SubscriptionStatus.Active,
            "past_due" => SubscriptionStatus.PastDue,
            "canceled" => SubscriptionStatus.Canceled,
            "incomplete" => SubscriptionStatus.Incomplete,
            _ => SubscriptionStatus.Active
        };

        // If webhook-deserialized dates are epoch zero, fetch authoritative dates from Stripe API
        var minValid = new DateTime(2020, 1, 1);
        var effectiveStart = periodStart;
        var effectiveEnd = periodEnd;
        var effectiveCancelAt = cancelAt;

        if (periodStart <= minValid || periodEnd <= minValid)
        {
            try
            {
                var stripeService = new Stripe.SubscriptionService();
                var stripeSub = await stripeService.GetAsync(stripeSubscriptionId);
                if (stripeSub.CurrentPeriodStart > minValid)
                    effectiveStart = stripeSub.CurrentPeriodStart;
                if (stripeSub.CurrentPeriodEnd > minValid)
                    effectiveEnd = stripeSub.CurrentPeriodEnd;
                if (stripeSub.CancelAtPeriodEnd && stripeSub.CurrentPeriodEnd > minValid)
                    effectiveCancelAt = stripeSub.CurrentPeriodEnd;
                else if (!stripeSub.CancelAtPeriodEnd)
                    effectiveCancelAt = null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch subscription {SubId} from Stripe API, using webhook dates", stripeSubscriptionId);
            }
        }

        if (effectiveStart > minValid)
            subscription.CurrentPeriodStart = effectiveStart;
        if (effectiveEnd > minValid)
            subscription.CurrentPeriodEnd = effectiveEnd;

        // Update CancelAt: valid date = set it, null = clear it, epoch = don't overwrite
        if (effectiveCancelAt.HasValue && effectiveCancelAt.Value > minValid)
            subscription.CancelAt = effectiveCancelAt;
        else if (!effectiveCancelAt.HasValue)
            subscription.CancelAt = null;
        // else: bad date, keep existing value

        subscription.UpdatedAt = DateTime.UtcNow;

        var player = await _context.Players.FindAsync(subscription.PlayerId);
        if (player != null)
        {
            player.CurrentTier = subscription.Status == SubscriptionStatus.Active
                ? subscription.Tier
                : SubscriptionTier.Free;
        }

        await _context.SaveChangesAsync();
    }

    public async Task CancelSubscriptionAsync(Guid playerId)
    {
        var subscription = await _context.Subscriptions
            .FirstOrDefaultAsync(s => s.PlayerId == playerId && s.Status == SubscriptionStatus.Active);

        if (subscription == null)
            throw new InvalidOperationException("No active subscription found.");

        if (!string.IsNullOrEmpty(subscription.StripeSubscriptionId))
        {
            var service = new Stripe.SubscriptionService();
            var stripeSub = await service.UpdateAsync(subscription.StripeSubscriptionId, new SubscriptionUpdateOptions
            {
                CancelAtPeriodEnd = true
            });

            // Use Stripe's authoritative dates instead of our potentially stale DB values
            subscription.CurrentPeriodStart = stripeSub.CurrentPeriodStart;
            subscription.CurrentPeriodEnd = stripeSub.CurrentPeriodEnd;
            subscription.CancelAt = stripeSub.CurrentPeriodEnd;
        }
        else
        {
            subscription.CancelAt = subscription.CurrentPeriodEnd;
        }

        subscription.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Subscription scheduled for cancellation for player {PlayerId}", playerId);
    }

    public async Task<Models.Domain.Subscription?> GetSubscriptionAsync(Guid playerId)
    {
        return await _context.Subscriptions.FirstOrDefaultAsync(s => s.PlayerId == playerId);
    }

    public async Task<string> CreateCustomerPortalSessionAsync(Guid playerId, string returnUrl)
    {
        var subscription = await _context.Subscriptions.FirstOrDefaultAsync(s => s.PlayerId == playerId);

        if (subscription == null || string.IsNullOrEmpty(subscription.StripeCustomerId))
            throw new InvalidOperationException("No Stripe customer found.");

        var options = new Stripe.BillingPortal.SessionCreateOptions
        {
            Customer = subscription.StripeCustomerId,
            ReturnUrl = returnUrl,
        };

        var service = new Stripe.BillingPortal.SessionService();
        var session = await service.CreateAsync(options);

        return session.Url;
    }

    private SubscriptionTier DetermineTierFromPriceId(string priceId)
    {
        if (priceId == _config["Stripe:PriceIds:Premium"])
            return SubscriptionTier.Premium;
        if (priceId == _config["Stripe:PriceIds:PremiumPlus"])
            return SubscriptionTier.PremiumPlus;
        return SubscriptionTier.Free;
    }
}
