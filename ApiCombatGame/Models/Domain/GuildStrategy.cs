using System.ComponentModel.DataAnnotations;

namespace ApiCombatGame.Models.Domain;

public class GuildStrategy
{
    [Key]
    public Guid Id { get; set; }

    public Guid GuildId { get; set; }
    public Guild Guild { get; set; } = null!;

    public Guid CreatorId { get; set; }
    public Player Creator { get; set; } = null!;

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    public string StrategyJson { get; set; } = "{}";

    public int UsageCount { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
