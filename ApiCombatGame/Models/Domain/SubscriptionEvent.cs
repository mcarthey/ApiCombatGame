using System.ComponentModel.DataAnnotations;
using ApiCombatGame.Models.Enums;

namespace ApiCombatGame.Models.Domain;

public class SubscriptionEvent
{
    [Key]
    public Guid Id { get; set; }

    public Guid PlayerId { get; set; }
    public Player Player { get; set; } = null!;

    [Required]
    [MaxLength(50)]
    public string EventType { get; set; } = string.Empty; // created, upgraded, downgraded, cancelled, renewed

    public SubscriptionTier? OldTier { get; set; }
    public SubscriptionTier? NewTier { get; set; }

    public decimal? AmountUsd { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
