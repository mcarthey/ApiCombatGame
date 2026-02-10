using System.ComponentModel.DataAnnotations;
using ApiCombatGame.Models.Enums;

namespace ApiCombatGame.Models.Domain;

public class Ability
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public AbilityType Type { get; set; }

    public int Damage { get; set; }
    public int Healing { get; set; }
    public int CooldownTurns { get; set; }

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Whether this ability targets allies (for healing) or enemies.
    /// </summary>
    public bool TargetsAllies { get; set; }

    /// <summary>
    /// Whether this ability hits all targets (AoE).
    /// </summary>
    public bool IsAoE { get; set; }

    // Relationships
    public Guid UnitId { get; set; }
    public Unit Unit { get; set; } = null!;
}
