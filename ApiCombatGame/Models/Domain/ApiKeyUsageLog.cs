using System.ComponentModel.DataAnnotations;

namespace ApiCombatGame.Models.Domain;

public class ApiKeyUsageLog
{
    [Key]
    public Guid Id { get; set; }

    public Guid ApiKeyId { get; set; }
    public ApiKey ApiKey { get; set; } = null!;

    [MaxLength(45)]
    public string IpAddress { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? UserAgent { get; set; }

    [MaxLength(200)]
    public string? Endpoint { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
