using System.Text.Json.Serialization;
using ApiCombatGame.Models.DTOs.Common;

namespace ApiCombatGame.Models.DTOs.Challenge;

/// <summary>A daily challenge objective with progress tracking and rewards.</summary>
public class ChallengeResponse
{
    /// <summary>Hypermedia links to related resources (claim reward when completed).</summary>
    [JsonPropertyName("_links")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, ApiLink>? Links { get; set; }

    /// <summary>Unique challenge identifier.</summary>
    public Guid ChallengeId { get; set; }

    /// <summary>Challenge title (e.g., "Warrior's Path", "Healing Hands").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>What you need to do to complete this challenge.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Difficulty tier: "easy", "medium", or "hard". Harder = better rewards.</summary>
    public string Difficulty { get; set; } = "medium";

    /// <summary>Battle pass XP awarded on completion.</summary>
    public int BattlePassXp { get; set; }

    /// <summary>Current progress toward completion.</summary>
    public int Progress { get; set; }

    /// <summary>Progress value needed to complete the challenge.</summary>
    public int RequiredProgress { get; set; }

    /// <summary>Completion percentage (0-100). Computed from progress / requiredProgress.</summary>
    public double ProgressPercentage => RequiredProgress > 0 ? (double)Progress / RequiredProgress * 100 : 0;

    /// <summary>Whether the challenge objective has been met. Claim your reward when true.</summary>
    public bool IsCompleted { get; set; }

    /// <summary>Currency reward for completing this challenge.</summary>
    public int RewardCurrency { get; set; }

    /// <summary>Experience points reward for completing this challenge.</summary>
    public int RewardExperience { get; set; }

    /// <summary>UTC timestamp when this challenge expires. Unclaimed rewards are forfeited.</summary>
    public DateTime ExpiresAt { get; set; }
}

/// <summary>Request to claim a completed challenge reward.</summary>
public class ClaimRequest
{
    /// <summary>The ID of the completed challenge to claim.</summary>
    public Guid ChallengeId { get; set; }
}
