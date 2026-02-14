using System.ComponentModel.DataAnnotations;

namespace ApiCombatGame.Models.Domain;

/// <summary>
/// Links a player account to a Discord user ID for bot integration.
/// </summary>
public class DiscordLink
{
    [Key]
    public Guid Id { get; set; }

    public Guid PlayerId { get; set; }
    public Player Player { get; set; } = null!;

    [Required]
    [MaxLength(30)]
    public string DiscordUserId { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? DiscordUsername { get; set; }

    /// <summary>Verification code used during linking process.</summary>
    [MaxLength(10)]
    public string? VerificationCode { get; set; }

    public bool IsVerified { get; set; } = false;

    /// <summary>Whether to receive Discord DMs for game notifications.</summary>
    public bool NotificationsEnabled { get; set; } = true;

    public DateTime LinkedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Registered Discord webhook URL for receiving game events.
/// </summary>
public class DiscordWebhook
{
    [Key]
    public Guid Id { get; set; }

    public Guid PlayerId { get; set; }
    public Player Player { get; set; } = null!;

    [Required]
    [MaxLength(300)]
    public string WebhookUrl { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Label { get; set; } = string.Empty;

    /// <summary>Comma-separated event types to subscribe to (e.g., "battle_won,level_up,guild_boss").</summary>
    [MaxLength(500)]
    public string EventTypes { get; set; } = "battle_won,battle_lost,level_up";

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
