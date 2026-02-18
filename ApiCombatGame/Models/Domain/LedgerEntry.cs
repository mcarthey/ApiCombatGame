using System.ComponentModel.DataAnnotations;

namespace ApiCombatGame.Models.Domain;

/// <summary>
/// Immutable audit record of a single numeric state change on a Player or Guild.
/// Used for support debugging — NOT a player-facing feature.
/// </summary>
public class LedgerEntry
{
    [Key]
    public Guid Id { get; set; }

    /// <summary>Player ID or Guild ID.</summary>
    public Guid EntityId { get; set; }

    /// <summary>"Player" or "Guild".</summary>
    [Required]
    [MaxLength(10)]
    public string EntityType { get; set; } = string.Empty;

    /// <summary>"Rating", "Currency", "ExperiencePoints", "Level", "WinStreak", "Gems", "AchievementPoints", "TreasuryBalance".</summary>
    [Required]
    [MaxLength(30)]
    public string Property { get; set; } = string.Empty;

    public long OldValue { get; set; }
    public long NewValue { get; set; }
    public long Delta { get; set; }

    /// <summary>System that caused the change: "Battle", "GuildBoss", "Achievement", etc.</summary>
    [Required]
    [MaxLength(30)]
    public string Source { get; set; } = string.Empty;

    /// <summary>Human-readable action: "BattleWon", "BossDefeated", "ItemPurchased", "AdminAdjust".</summary>
    [Required]
    [MaxLength(50)]
    public string Action { get; set; } = string.Empty;

    /// <summary>Battle ID, achievement ID, boss ID, tournament ID, etc.</summary>
    public Guid? RelatedEntityId { get; set; }

    /// <summary>Extra context for support debugging.</summary>
    [MaxLength(500)]
    public string? ContextJson { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
