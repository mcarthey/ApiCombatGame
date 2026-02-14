using System.ComponentModel.DataAnnotations;

namespace ApiCombatGame.Models.Domain;

public class Season
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public int SeasonNumber { get; set; }
    public bool IsActive { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    /// <summary>JSON-serialized end-of-season reward tiers (currency per tier).</summary>
    public string RewardConfigJson { get; set; } = "{}";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public List<PlayerSeasonRank> PlayerRanks { get; set; } = new();
}
