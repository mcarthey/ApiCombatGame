using System.ComponentModel.DataAnnotations;

namespace ApiCombatGame.Models.Domain;

/// <summary>
/// A single activity event in a player's feed (battle results, achievements, level-ups, etc.).
/// </summary>
public class ActivityFeedEntry
{
    [Key]
    public Guid Id { get; set; }

    public Guid PlayerId { get; set; }
    public Player Player { get; set; } = null!;

    /// <summary>"battle_won", "battle_lost", "level_up", "achievement", "unit_unlocked", "guild_joined", "tournament_win", "rival_defeated", "loot_drop"</summary>
    [Required]
    [MaxLength(30)]
    public string ActivityType { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Description { get; set; } = string.Empty;

    /// <summary>Optional related entity (battle ID, achievement ID, etc.).</summary>
    public Guid? RelatedEntityId { get; set; }

    /// <summary>Optional JSON metadata for rich display.</summary>
    [MaxLength(1000)]
    public string? MetadataJson { get; set; }

    /// <summary>Whether this activity is visible to other players.</summary>
    public bool IsPublic { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
