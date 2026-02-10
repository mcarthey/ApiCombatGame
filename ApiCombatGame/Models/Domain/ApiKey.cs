using System.ComponentModel.DataAnnotations;

namespace ApiCombatGame.Models.Domain;

public class ApiKey
{
    [Key]
    public Guid Id { get; set; }

    public Guid PlayerId { get; set; }
    public Player Player { get; set; } = null!;

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string KeyHash { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string KeyPrefix { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
    public DateTime? LastUsedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RevokedAt { get; set; }
}
