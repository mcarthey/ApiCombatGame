using System.ComponentModel.DataAnnotations;

namespace ApiCombatGame.Models.Domain;

public class DailyChallenge
{
    [Key]
    public Guid Id { get; set; }

    public Guid PlayerId { get; set; }
    public Player Player { get; set; } = null!;

    /// <summary>
    /// Class name of the challenge generator that created this (e.g., "TeamComposition", "WinStreak").
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string ChallengeType { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Serialized requirements specific to the challenge type.
    /// </summary>
    public string RequirementsJson { get; set; } = "{}";

    /// <summary>"easy", "medium", or "hard". Harder challenges give better rewards.</summary>
    [MaxLength(20)]
    public string Difficulty { get; set; } = "medium";

    public int Progress { get; set; } = 0;
    public int RequiredProgress { get; set; }
    public bool IsCompleted { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    // Rewards
    public int RewardCurrency { get; set; }
    public int RewardExperience { get; set; }
}
