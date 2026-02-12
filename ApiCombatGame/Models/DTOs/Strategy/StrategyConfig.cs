namespace ApiCombatGame.Models.DTOs.Strategy;

/// <summary>Declarative battle strategy that controls how your team's AI behaves in combat. Assign this to a team to automate tactical decisions.</summary>
public class StrategyConfig
{
    /// <summary>Formation type controlling positioning and stat bonuses. Options: "aggressive" (+attack), "defensive" (+defense), "balanced" (no bonus).</summary>
    public string Formation { get; set; } = "balanced";

    /// <summary>Ordered list of target selection priorities evaluated top-to-bottom. Options: "lowest_hp", "highest_hp", "healers", "highest_threat", "random".</summary>
    public List<string> TargetPriority { get; set; } = new() { "lowest_hp" };

    /// <summary>Conditional ability rules keyed by ability name. Controls when and how each ability is used during battle.</summary>
    public Dictionary<string, AbilityCondition> Abilities { get; set; } = new();
}

/// <summary>Conditional rule defining when and how a specific ability should be used.</summary>
public class AbilityCondition
{
    /// <summary>Condition that must be true to use this ability. Options: "always", "ally_hp_below_50", "ally_hp_below_25", "enemy_count_gte_2", "enemy_count_gte_3", "cooldown_ready".</summary>
    public string When { get; set; } = "always";

    /// <summary>How to select the target. Options: "priority" (use TargetPriority list), "lowest_ally_hp", "self", "all_enemies", "all_allies".</summary>
    public string Target { get; set; } = "priority";
}
