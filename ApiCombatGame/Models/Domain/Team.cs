using System.ComponentModel.DataAnnotations;

namespace ApiCombatGame.Models.Domain;

public class Team
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public Guid PlayerId { get; set; }
    public Player Player { get; set; } = null!;

    /// <summary>
    /// JSON-serialized list of Unit IDs (max 5).
    /// </summary>
    public string UnitIdsJson { get; set; } = "[]";

    /// <summary>
    /// JSON-serialized StrategyConfig.
    /// </summary>
    public string StrategyJson { get; set; } = "{}";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
