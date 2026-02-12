namespace ApiCombatGame.Models.DTOs.Progression;

/// <summary>Detailed breakdown of rewards earned from a battle.</summary>
public class BattleRewardsSummary
{
    public int GoldEarned { get; set; }
    public int XpEarned { get; set; }
    public int? NewLevel { get; set; }
    public bool FirstBattleBonus { get; set; }
    public int WinStreak { get; set; }
    public int WinStreakBonus { get; set; }
    public decimal TierMultiplier { get; set; }
}
