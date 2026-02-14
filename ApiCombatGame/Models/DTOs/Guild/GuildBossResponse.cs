using System.Text.Json.Serialization;
using ApiCombatGame.Models.DTOs.Common;

namespace ApiCombatGame.Models.DTOs.Guild;

/// <summary>Current state of a guild's raid boss encounter.</summary>
public class GuildBossResponse
{
    /// <summary>Hypermedia links to related resources (attack, leaderboard).</summary>
    [JsonPropertyName("_links")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, ApiLink>? Links { get; set; }

    /// <summary>Unique boss encounter identifier.</summary>
    public Guid BossId { get; set; }

    /// <summary>Boss name (e.g., "Infernal Titan", "Void Reaper").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Flavor text describing the boss and its abilities.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Boss's maximum hit points at full health.</summary>
    public int MaxHp { get; set; }

    /// <summary>Boss's remaining hit points. Reduced by guild member attacks.</summary>
    public int CurrentHp { get; set; }

    /// <summary>Remaining HP as a percentage (0-100). Computed from currentHp / maxHp.</summary>
    public double HpPercentage => MaxHp > 0 ? (double)CurrentHp / MaxHp * 100 : 0;

    /// <summary>UTC timestamp when this boss encounter expires. Defeat it before time runs out.</summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>Whether the boss has been defeated by the guild.</summary>
    public bool IsDefeated { get; set; }

    /// <summary>Currency reward distributed to participants when the boss is defeated.</summary>
    public int RewardCurrency { get; set; }

    /// <summary>Experience reward distributed to participants when the boss is defeated.</summary>
    public int RewardExperience { get; set; }
}

/// <summary>Request to attack a guild boss with a specific team.</summary>
public class BossAttemptRequest
{
    /// <summary>The boss encounter to attack.</summary>
    public Guid BossId { get; set; }

    /// <summary>Your team to send into the raid.</summary>
    public Guid TeamId { get; set; }
}

/// <summary>Result of a guild boss attack attempt.</summary>
public class BossAttemptResponse
{
    /// <summary>Unique attempt identifier for this attack.</summary>
    public Guid AttemptId { get; set; }

    /// <summary>Name of the attacking player.</summary>
    public string PlayerName { get; set; } = string.Empty;

    /// <summary>Total damage dealt to the boss in this attempt.</summary>
    public int DamageDealt { get; set; }

    /// <summary>Whether this attack delivered the final killing blow.</summary>
    public bool WasKillingBlow { get; set; }

    /// <summary>UTC timestamp of the attack.</summary>
    public DateTime AttemptedAt { get; set; }
}
