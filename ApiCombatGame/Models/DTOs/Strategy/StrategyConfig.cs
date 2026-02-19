namespace ApiCombatGame.Models.DTOs.Strategy;

/// <summary>Declarative battle strategy that controls how your team's AI behaves in combat. Assign this to a team to automate tactical decisions.</summary>
public class StrategyConfig
{
    /// <summary>Formation type controlling positioning and stat bonuses.</summary>
    public Formation Formation { get; set; } = Formation.balanced;

    /// <summary>Ordered list of target selection priorities evaluated top-to-bottom.</summary>
    public List<TargetPriority> TargetPriority { get; set; } = new() { Strategy.TargetPriority.lowest_hp };

    /// <summary>Conditional ability rules keyed by ability name. Controls when and how each ability is used during battle.</summary>
    public Dictionary<string, AbilityCondition> Abilities { get; set; } = new();
}

/// <summary>Conditional rule defining when and how a specific ability should be used.</summary>
public class AbilityCondition
{
    /// <summary>Condition that must be true to use this ability.</summary>
    public AbilityWhen When { get; set; } = AbilityWhen.always;

    /// <summary>How to select the target for this ability.</summary>
    public AbilityTarget Target { get; set; } = AbilityTarget.priority;
}
