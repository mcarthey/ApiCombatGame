namespace ApiCombatGame.Models.DTOs.Strategy;

public class StrategyConfig
{
    /// <summary>
    /// Formation type: "aggressive", "defensive", "balanced"
    /// </summary>
    public string Formation { get; set; } = "balanced";

    /// <summary>
    /// Target priority order, e.g. ["lowest_hp", "healers", "highest_threat"]
    /// </summary>
    public List<string> TargetPriority { get; set; } = new() { "lowest_hp" };

    /// <summary>
    /// Ability conditions keyed by ability name.
    /// </summary>
    public Dictionary<string, AbilityCondition> Abilities { get; set; } = new();
}

public class AbilityCondition
{
    /// <summary>
    /// When to use: "always", "ally_hp_below_50", "enemy_count_gte_2"
    /// </summary>
    public string When { get; set; } = "always";

    /// <summary>
    /// Target selection: "priority", "lowest_ally_hp", "all_enemies"
    /// </summary>
    public string Target { get; set; } = "priority";
}
