using System.Text.Json.Serialization;
using ApiCombatGame.Models.Domain;
using ApiCombatGame.Models.DTOs.Common;

namespace ApiCombatGame.Models.DTOs.Battle;

/// <summary>Detailed outcome of a completed battle, including the full combat log.</summary>
public class BattleResultResponse
{
    /// <summary>Hypermedia links to related resources (replay, player profiles, queue again).</summary>
    [JsonPropertyName("_links")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, ApiLink>? Links { get; set; }

    /// <summary>Unique battle identifier.</summary>
    public Guid BattleId { get; set; }

    /// <summary>Final battle state. "Completed" for finished battles.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Player ID of the winner. Null for draws.</summary>
    public Guid? WinnerId { get; set; }

    /// <summary>Player ID of the loser. Null for draws.</summary>
    public Guid? LoserId { get; set; }

    /// <summary>Total number of turns the battle lasted (max 50).</summary>
    public int Turns { get; set; }

    /// <summary>Turn-by-turn combat log. Each entry records one action by one unit.</summary>
    public List<BattleLogEntry> BattleLog { get; set; } = new();

    /// <summary>Currency and rating rewards earned from this battle. Null if battle was cancelled.</summary>
    public BattleRewards? Rewards { get; set; }

    /// <summary>UTC timestamp when the battle finished.</summary>
    public DateTime? CompletedAt { get; set; }
}

/// <summary>Rewards earned from a completed battle.</summary>
public class BattleRewards
{
    /// <summary>In-game currency earned. Winners earn more than losers.</summary>
    public int Currency { get; set; }

    /// <summary>API rating change. Positive for wins, negative for losses. Zero in casual mode.</summary>
    public int RatingChange { get; set; }

    /// <summary>Experience points earned from this battle.</summary>
    public int ExperienceEarned { get; set; }

    /// <summary>Current win streak count after this battle.</summary>
    public int WinStreak { get; set; }

    /// <summary>Whether the first-battle-of-day bonus was applied.</summary>
    public bool FirstBattleBonus { get; set; }

    /// <summary>New player level after this battle, if a level-up occurred.</summary>
    public int? NewLevel { get; set; }

    /// <summary>Tier-based gold multiplier applied (1.0x Free, 1.5x Premium, 2.0x Premium+).</summary>
    public decimal TierMultiplier { get; set; }
}
