namespace ApiCombatGame.Models.ViewModels;

public class PlayerAnalyticsViewModel
{
    // Overall Stats
    public int TotalBattles { get; set; }
    public int TotalWins { get; set; }
    public int TotalLosses { get; set; }
    public decimal WinRate { get; set; }
    public int CurrentWinStreak { get; set; }
    public int LongestWinStreak { get; set; }
    public int CurrentLossStreak { get; set; }
    public int LongestLossStreak { get; set; }

    // Performance Metrics
    public decimal AverageTurnsPerBattle { get; set; }
    public decimal AverageRatingChangePerWin { get; set; }
    public decimal AverageRatingChangePerLoss { get; set; }
    public int TotalCurrencyEarned { get; set; }
    public int HighestRatingChange { get; set; }
    public int LowestRatingChange { get; set; }

    // Time-based Analytics
    public int BattlesToday { get; set; }
    public int BattlesThisWeek { get; set; }
    public int BattlesThisMonth { get; set; }
    public int WinsToday { get; set; }
    public int WinsThisWeek { get; set; }
    public int WinsThisMonth { get; set; }

    // Most Used Classes (top 5)
    public List<ClassUsageData> MostUsedClasses { get; set; } = new();

    // Recent Performance (last 20 battles)
    public List<BattleResultData> RecentBattles { get; set; } = new();

    // Win Rate by Class
    public List<ClassWinRateData> WinRateByClass { get; set; } = new();

    // Peak Performance
    public DateTime? BestDayDate { get; set; }
    public int BestDayWins { get; set; }
    public DateTime? MostActiveDayDate { get; set; }
    public int MostActiveDayBattles { get; set; }
}

public class ClassUsageData
{
    public string ClassName { get; set; } = string.Empty;
    public int TimesUsed { get; set; }
    public decimal Percentage { get; set; }
}

public class BattleResultData
{
    public DateTime CompletedAt { get; set; }
    public bool IsWin { get; set; }
    public int RatingChange { get; set; }
    public string Mode { get; set; } = "ranked";
    public int Turns { get; set; }
    public string OpponentName { get; set; } = string.Empty;
}

public class ClassWinRateData
{
    public string ClassName { get; set; } = string.Empty;
    public int Wins { get; set; }
    public int Losses { get; set; }
    public decimal WinRate { get; set; }
    public int TotalBattles { get; set; }
}
