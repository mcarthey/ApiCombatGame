namespace ApiCombatGame.Models.Enums;

/// <summary>
/// Current state of a player's premium subscription.
/// </summary>
public enum SubscriptionStatus
{
    /// <summary>Subscription is active and in good standing.</summary>
    Active,
    /// <summary>Payment failed but subscription is in a grace period.</summary>
    PastDue,
    /// <summary>Subscription has been canceled. Features revert to free tier at period end.</summary>
    Canceled,
    /// <summary>Initial payment has not completed. Subscription is not yet active.</summary>
    Incomplete
}
