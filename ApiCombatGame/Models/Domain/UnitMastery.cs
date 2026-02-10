using System.ComponentModel.DataAnnotations;

namespace ApiCombatGame.Models.Domain;

public class UnitMastery
{
    [Key]
    public Guid Id { get; set; }

    public Guid PlayerId { get; set; }
    public Player Player { get; set; } = null!;

    /// <summary>
    /// References the unit template ID, not a specific player's unit instance.
    /// </summary>
    public Guid UnitId { get; set; }

    public int Level { get; set; } = 1;
    public int ExperiencePoints { get; set; } = 0;
    public int BattlesUsed { get; set; } = 0;
    public int WinsWithUnit { get; set; } = 0;

    public DateTime FirstUsed { get; set; } = DateTime.UtcNow;
    public DateTime LastUsed { get; set; } = DateTime.UtcNow;
}
