using System.ComponentModel.DataAnnotations;

namespace ApiCombatGame.Models.Domain;

public class BattleReplay
{
    [Key]
    public Guid Id { get; set; }

    public Guid BattleId { get; set; }
    public Battle Battle { get; set; } = null!;

    /// <summary>
    /// Short URL for sharing (e.g., base62-encoded ID).
    /// </summary>
    [MaxLength(50)]
    public string ShareUrl { get; set; } = string.Empty;

    public int ViewCount { get; set; } = 0;
    public bool IsFeatured { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? FeaturedAt { get; set; }
}
