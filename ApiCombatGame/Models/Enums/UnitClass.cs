namespace ApiCombatGame.Models.Enums;

/// <summary>
/// Combat specialization of a unit. Each class has distinct stat distributions and abilities.
/// </summary>
public enum UnitClass
{
    /// <summary>Balanced melee combatant with strong attack and moderate defense.</summary>
    Warrior,
    /// <summary>Ranged spellcaster with high damage abilities but low health.</summary>
    Mage,
    /// <summary>Ranged physical attacker with high speed and precision strikes.</summary>
    Ranger,
    /// <summary>Support specialist with healing and buff abilities. Low attack power.</summary>
    Healer,
    /// <summary>Frontline defender with the highest health and defense. Low speed.</summary>
    Tank
}
