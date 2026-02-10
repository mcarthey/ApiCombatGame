using System.ComponentModel.DataAnnotations;

namespace ApiCombatGame.Models.Domain;

public class Strategy
{
    [Key]
    public Guid Id { get; set; }

    public Guid CreatorId { get; set; }
    public Player Creator { get; set; } = null!;

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// The actual strategy configuration JSON.
    /// </summary>
    public string StrategyJson { get; set; } = "{}";

    /// <summary>
    /// Price in currency. 0 = free.
    /// </summary>
    public int Price { get; set; } = 0;

    public bool IsPublic { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Metadata
    public int DownloadCount { get; set; } = 0;
    public int WinCount { get; set; } = 0;
    public int LossCount { get; set; } = 0;
    public double AverageRating { get; set; } = 0.0;

    /// <summary>
    /// Decay system: multiplier decreases over time to prevent meta stagnation.
    /// Formula: multiplier = max(0.5, 1.0 - (ageInWeeks * 0.05))
    /// </summary>
    public double EffectivenessMultiplier { get; set; } = 1.0;
    public DateTime LastDecayUpdate { get; set; } = DateTime.UtcNow;

    // Navigation
    public List<StrategyRating> Ratings { get; set; } = new();
}
