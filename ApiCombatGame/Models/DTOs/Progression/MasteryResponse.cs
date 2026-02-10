namespace ApiCombatGame.Models.DTOs.Progression;

public class MasteryResponse
{
    public Guid UnitId { get; set; }
    public int Level { get; set; }
    public int ExperiencePoints { get; set; }
    public int BattlesUsed { get; set; }
    public int WinsWithUnit { get; set; }
    public double WinRate => BattlesUsed > 0 ? (double)WinsWithUnit / BattlesUsed * 100 : 0;
}
