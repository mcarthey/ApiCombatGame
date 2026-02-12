using System.ComponentModel.DataAnnotations;
using ApiCombatGame.Models.Enums;

namespace ApiCombatGame.Models.Domain;

public class AdminAlert
{
    [Key]
    public Guid Id { get; set; }

    public AlertSeverity Severity { get; set; }

    [Required]
    [MaxLength(50)]
    public string Category { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Message { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? ActionRequired { get; set; }

    public bool IsAcknowledged { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
