using System.ComponentModel.DataAnnotations;
using ApiCombatGame.Models.Enums;

namespace ApiCombatGame.Models.Domain;

public class Unit
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public UnitClass Class { get; set; }
    public int Level { get; set; } = 1;

    // Base stats
    public int Health { get; set; }
    public int Attack { get; set; }
    public int Defense { get; set; }
    public int Speed { get; set; }

    /// <summary>
    /// Cost to unlock this unit.
    /// </summary>
    public int UnlockCost { get; set; } = 200;

    /// <summary>
    /// Whether this is a template unit (not owned by any player) used for seeding.
    /// </summary>
    public bool IsTemplate { get; set; }

    // Relationships
    public Guid? PlayerId { get; set; }
    public Player? Player { get; set; }

    public List<Ability> Abilities { get; set; } = new();
}
