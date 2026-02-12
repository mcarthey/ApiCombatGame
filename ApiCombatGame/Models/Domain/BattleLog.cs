namespace ApiCombatGame.Models.Domain;

/// <summary>
/// Represents a single entry in the battle log.
/// </summary>
public class BattleLogEntry
{
    /// <summary>The turn number (1-based) when this action occurred.</summary>
    public int Turn { get; set; }

    /// <summary>Name of the unit performing the action.</summary>
    public string Actor { get; set; } = string.Empty;

    /// <summary>The ability or action used (e.g., "Fireball", "Shield Wall", "Basic Attack").</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>Name of the target unit.</summary>
    public string Target { get; set; } = string.Empty;

    /// <summary>Damage dealt to the target. 0 for non-offensive actions.</summary>
    public int Damage { get; set; }

    /// <summary>HP restored to the target. 0 for non-healing actions.</summary>
    public int Healing { get; set; }

    /// <summary>Status effects applied (e.g., "Stunned", "Defense Up").</summary>
    public List<string> Effects { get; set; } = new();

    /// <summary>The target's remaining HP after this action resolved.</summary>
    public int TargetHpRemaining { get; set; }
}
