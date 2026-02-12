using ApiCombatGame.Models.Enums;

namespace ApiCombatGame.Filters;

/// <summary>
/// Marks a controller action as requiring a minimum subscription tier.
/// Used by TierGatingActionFilter to enforce tier-based access control.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class RequiresTierAttribute : Attribute
{
    public SubscriptionTier MinimumTier { get; }

    public RequiresTierAttribute(SubscriptionTier minimumTier)
    {
        MinimumTier = minimumTier;
    }
}
