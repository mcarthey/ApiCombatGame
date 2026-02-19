using System.ComponentModel;
using System.Text.Json.Serialization;

namespace ApiCombatGame.Models.DTOs.Strategy;

/// <summary>Formation type controlling positioning and stat bonuses during battle.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Formation
{
    [Description("+15% attack bonus")]
    aggressive,

    [Description("-15% damage taken")]
    defensive,

    [Description("No stat bonuses")]
    balanced
}

/// <summary>Target selection priority values evaluated top-to-bottom during combat.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TargetPriority
{
    [Description("Enemy with lowest current HP")]
    lowest_hp,

    [Description("Enemy Healers first")]
    healers,

    [Description("Enemy with highest Attack stat")]
    highest_threat,

    [Description("Enemy Mages first")]
    mages,

    [Description("Enemy Tanks first")]
    tanks
}

/// <summary>Condition that must be true for an ability rule to fire.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AbilityWhen
{
    [Description("Always use when off cooldown")]
    always,

    [Description("Any ally below 50% HP")]
    ally_hp_below_50,

    [Description("Any ally below 30% HP")]
    ally_hp_below_30,

    [Description("2+ enemies alive (good for AoE)")]
    enemy_count_gte_2,

    [Description("3+ enemies alive")]
    enemy_count_gte_3,

    [Description("Caster's own HP below 50%")]
    self_hp_below_50
}

/// <summary>How to select the target for an ability rule.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AbilityTarget
{
    [Description("Uses your targetPriority list")]
    priority,

    [Description("Enemy with lowest HP")]
    lowest_hp,

    [Description("Enemy with highest Attack")]
    highest_threat,

    [Description("All enemies (AoE abilities)")]
    all_enemies,

    [Description("Ally with lowest HP (for heals)")]
    lowest_ally_hp,

    [Description("The casting unit itself")]
    self,

    [Description("All allies (AoE heals/buffs)")]
    all_allies
}
