using System.ComponentModel.DataAnnotations;

namespace ApiCombatGame.Models.Domain;

public class AdminAuditLog
{
    [Key]
    public Guid Id { get; set; }

    public Guid AdminPlayerId { get; set; }
    public Player AdminPlayer { get; set; } = null!;

    [Required]
    [MaxLength(100)]
    public string Action { get; set; } = string.Empty;

    public Guid? TargetPlayerId { get; set; }
    public Player? TargetPlayer { get; set; }

    /// <summary>JSON with additional context about the action.</summary>
    [MaxLength(2000)]
    public string? DetailsJson { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
