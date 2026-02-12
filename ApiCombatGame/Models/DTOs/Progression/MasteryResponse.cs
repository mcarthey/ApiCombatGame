namespace ApiCombatGame.Models.DTOs.Progression;

/// <summary>A player's mastery progression with a specific unit.</summary>
public class MasteryResponse
{
    /// <summary>The unit this mastery record belongs to.</summary>
    public Guid UnitId { get; set; }

    /// <summary>Current mastery level. Increases with experience.</summary>
    public int Level { get; set; }

    /// <summary>Total experience points earned with this unit.</summary>
    public int ExperiencePoints { get; set; }

    /// <summary>Total number of battles this unit has participated in.</summary>
    public int BattlesUsed { get; set; }

    /// <summary>Number of battles won while using this unit.</summary>
    public int WinsWithUnit { get; set; }

    /// <summary>Win rate percentage (0-100) with this unit. Computed from wins / battles.</summary>
    public double WinRate => BattlesUsed > 0 ? (double)WinsWithUnit / BattlesUsed * 100 : 0;
}
