namespace ApiCombatGame.Models.Enums;

/// <summary>
/// Subscription plan level determining feature access and battle limits.
/// </summary>
public enum SubscriptionTier
{
    /// <summary>Free tier. 10 battles per day, 3 team slots, standard matchmaking.</summary>
    Free,
    /// <summary>Premium tier. Unlimited battles, 10 team slots, priority matchmaking.</summary>
    Premium,
    /// <summary>Premium Plus tier. All Premium benefits plus early access to new units and bonus rewards.</summary>
    PremiumPlus
}
