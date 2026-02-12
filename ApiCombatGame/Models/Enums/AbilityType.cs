namespace ApiCombatGame.Models.Enums;

/// <summary>
/// Tier of a unit's combat ability, indicating its power level and cooldown.
/// </summary>
public enum AbilityType
{
    /// <summary>Standard attack with no cooldown. Available every turn.</summary>
    BasicAttack,
    /// <summary>Class-specific ability with moderate power and a short cooldown.</summary>
    ClassAbility,
    /// <summary>Most powerful ability with high impact but a long cooldown.</summary>
    Ultimate
}
