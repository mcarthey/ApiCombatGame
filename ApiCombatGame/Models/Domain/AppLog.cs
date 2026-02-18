using System.ComponentModel.DataAnnotations;
using ApiCombatGame.Models.Enums;

namespace ApiCombatGame.Models.Domain;

public class AppLog
{
    [Key]
    public Guid Id { get; set; }

    public AppLogLevel Level { get; set; }

    [Required]
    [MaxLength(100)]
    public string Category { get; set; } = string.Empty;

    [Required]
    [MaxLength(2000)]
    public string Message { get; set; } = string.Empty;

    [MaxLength(8000)]
    public string? ExceptionDetails { get; set; }

    [MaxLength(8000)]
    public string? StackTrace { get; set; }

    [MaxLength(20)]
    public string? SupportId { get; set; }

    public Guid? PlayerId { get; set; }
    public Player? Player { get; set; }

    [MaxLength(500)]
    public string? RequestPath { get; set; }

    [MaxLength(10)]
    public string? HttpMethod { get; set; }

    [MaxLength(45)]
    public string? IpAddress { get; set; }

    [MaxLength(500)]
    public string? UserAgent { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
