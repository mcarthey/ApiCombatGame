using System.ComponentModel.DataAnnotations;

namespace ApiCombatGame.Models.Domain;

public class EnvironmentalModifier
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Class name of the modifier implementation (e.g., "ArcaneDisruption", "HeavyArmor").
    /// Allows dynamic loading of modifier effects without schema changes.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string ModifierType { get; set; } = string.Empty;

    /// <summary>
    /// Serialized JSON config for the modifier. Each modifier type can define its own config shape.
    /// </summary>
    public string ConfigJson { get; set; } = "{}";

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
