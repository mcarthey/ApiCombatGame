using System.ComponentModel.DataAnnotations;

namespace ApiCombatGame.Models.Domain;

public class EasterEgg
{
    [Key]
    public Guid Id { get; set; }

    /// <summary>Unique redemption code, e.g. "CHEST-7Kx9".</summary>
    [Required]
    [MaxLength(20)]
    public string Code { get; set; } = string.Empty;

    /// <summary>Page path where the egg is hidden, e.g. "/api-docs/v1".</summary>
    [Required]
    [MaxLength(200)]
    public string PagePath { get; set; } = string.Empty;

    /// <summary>CSS selector for the container element the egg is placed near.</summary>
    [MaxLength(200)]
    public string CssSelector { get; set; } = string.Empty;

    /// <summary>Horizontal offset within the container (0–100%).</summary>
    public double OffsetX { get; set; }

    /// <summary>Vertical offset within the container (0–100%).</summary>
    public double OffsetY { get; set; }

    /// <summary>Monday of the week this egg is active.</summary>
    public DateTime WeekStart { get; set; }

    /// <summary>End of the active window (Sunday 23:59 UTC).</summary>
    public DateTime WeekEnd { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>Optional hint shown after multiple visits without finding it.</summary>
    [MaxLength(200)]
    public string? HintText { get; set; }

    /// <summary>1 = easy (obvious pages), 2 = medium, 3 = hard (deep in docs). Affects loot rarity.</summary>
    public int Difficulty { get; set; } = 1;

    /// <summary>Material icon name for the hidden egg, e.g. "mystery", "egg_alt".</summary>
    [MaxLength(50)]
    public string IconName { get; set; } = "egg_alt";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public List<EggClaim> Claims { get; set; } = new();
}
