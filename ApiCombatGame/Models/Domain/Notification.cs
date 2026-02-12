using System.ComponentModel.DataAnnotations;
using ApiCombatGame.Models.Enums;

namespace ApiCombatGame.Models.Domain;

public class Notification
{
    [Key]
    public Guid Id { get; set; }

    public Guid PlayerId { get; set; }
    public Player Player { get; set; } = null!;

    public NotificationType Type { get; set; }
    public NotificationCategory Category { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Message { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? ActionUrl { get; set; }

    public bool IsRead { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }
}
