using System.ComponentModel.DataAnnotations;

namespace ApiCombatGame.Models.Domain;

public class Achievement
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(500)]
    public string IconUrl { get; set; } = string.Empty;

    /// <summary>
    /// Category: "Combat", "Collection", "Social", etc.
    /// </summary>
    [MaxLength(50)]
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Achievement points awarded on unlock.
    /// </summary>
    public int Points { get; set; }

    /// <summary>
    /// Serialized unlock requirements. Structure depends on achievement type.
    /// </summary>
    public string RequirementsJson { get; set; } = "{}";

    /// <summary>
    /// Hidden achievements are not shown until unlocked.
    /// </summary>
    public bool IsSecret { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public List<PlayerAchievement> PlayerAchievements { get; set; } = new();
}
