using System.Text.Json.Serialization;
using ApiCombatGame.Models.DTOs.Common;
using ApiCombatGame.Models.DTOs.Strategy;

namespace ApiCombatGame.Models.DTOs.Team;

/// <summary>A configured team with its units and battle strategy.</summary>
public class TeamResponse
{
    /// <summary>Hypermedia links to related resources (queue battle, delete team).</summary>
    [JsonPropertyName("_links")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, ApiLink>? Links { get; set; }

    /// <summary>Unique team identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Team display name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>The units assigned to this team with their current stats.</summary>
    public List<UnitSummary> Units { get; set; } = new();

    /// <summary>Battle strategy configuration. Null if using default AI behavior.</summary>
    public StrategyConfig? Strategy { get; set; }

    /// <summary>When this team was first created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>When this team was last modified.</summary>
    public DateTime UpdatedAt { get; set; }
}

/// <summary>Compact view of a unit's identity and combat stats.</summary>
public class UnitSummary
{
    /// <summary>Unique unit identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Unit display name (e.g., "Shadow Mage", "Iron Guardian").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Combat class: Warrior, Mage, Ranger, Healer, or Tank.</summary>
    public string Class { get; set; } = string.Empty;

    /// <summary>Current unit level. Higher levels have better stats.</summary>
    public int Level { get; set; }

    /// <summary>Maximum hit points. When reduced to 0, the unit is eliminated.</summary>
    public int Health { get; set; }

    /// <summary>Offensive power. Increases damage dealt by abilities.</summary>
    public int Attack { get; set; }

    /// <summary>Damage mitigation. Reduces incoming damage.</summary>
    public int Defense { get; set; }

    /// <summary>Turn order priority. Faster units act first each turn.</summary>
    public int Speed { get; set; }
}
