namespace ApiCombatGame.Models.ViewModels;

public class DashboardViewModel
{
    public Guid PlayerId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string CurrentTier { get; set; } = "Free";
    public int BattlesToday { get; set; }
    public int DailyLimit { get; set; } = 10;
    public double WinRate { get; set; }
    public int GlobalRank { get; set; }
    public int Rating { get; set; }
    public int RatingChange { get; set; }
    public int Currency { get; set; }
    public int Level { get; set; }
    public List<RecentBattleInfo> RecentBattles { get; set; } = new();
}

public class RecentBattleInfo
{
    public Guid BattleId { get; set; }
    public string OpponentName { get; set; } = string.Empty;
    public bool IsWin { get; set; }
    public int RatingChange { get; set; }
    public DateTime CompletedAt { get; set; }
}
