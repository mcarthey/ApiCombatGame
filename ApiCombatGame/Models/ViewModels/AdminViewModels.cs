namespace ApiCombatGame.Models.ViewModels;

// Overview Dashboard
public class AdminOverviewData
{
    public int TotalPlayers { get; set; }
    public int Dau { get; set; }
    public int Wau { get; set; }
    public int Mau { get; set; }
    public double DauChange { get; set; }
    public double WauChange { get; set; }
    public double MauChange { get; set; }
    public decimal Mrr { get; set; }
    public decimal Arr { get; set; }
    public int BattlesToday { get; set; }
    public int BattlesThisWeek { get; set; }
    public int SignupsToday { get; set; }
    public int SignupsThisWeek { get; set; }
    public int FreeTierCount { get; set; }
    public int PremiumCount { get; set; }
    public int PremiumPlusCount { get; set; }
    public double ConversionRate { get; set; }
    public int TotalGuilds { get; set; }
    public double DauMauRatio { get; set; }
}

// Player Analytics
public class AdminPlayerAnalyticsData
{
    public List<AdminPlayerSummary> Players { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

public class AdminPlayerSummary
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int Level { get; set; }
    public int Rating { get; set; }
    public int Currency { get; set; }
    public string CurrentTier { get; set; } = "Free";
    public int WinStreak { get; set; }
    public int TotalBattles { get; set; }
    public double WinRate { get; set; }
    public bool IsAdmin { get; set; }
    public bool IsBot { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastLoginAt { get; set; }
}

// Player Detail
public class AdminPlayerDetailData
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int Level { get; set; }
    public int Rating { get; set; }
    public int Currency { get; set; }
    public int ExperiencePoints { get; set; }
    public int WinStreak { get; set; }
    public string CurrentTier { get; set; } = "Free";
    public bool IsAdmin { get; set; }
    public string AdminRole { get; set; } = "None";
    public DateTime CreatedAt { get; set; }
    public DateTime LastLoginAt { get; set; }
    public int TotalBattles { get; set; }
    public int Wins { get; set; }
    public double WinRate { get; set; }
    public int UnitsOwned { get; set; }
    public int TeamsConfigured { get; set; }
    public int AchievementsUnlocked { get; set; }
    public string? GuildName { get; set; }
    public string? GuildRole { get; set; }
    public string? SubscriptionStatus { get; set; }
    public List<AdminBattleInfo> RecentBattles { get; set; } = new();
}

public class AdminBattleInfo
{
    public Guid Id { get; set; }
    public Guid? OpponentId { get; set; }
    public string OpponentName { get; set; } = "Unknown";
    public bool IsWin { get; set; }
    public int RatingChange { get; set; }
    public DateTime CompletedAt { get; set; }
}

// Meta/Balance
public class AdminMetaData
{
    public int TotalBattlesInPeriod { get; set; }
    public int Days { get; set; }
    public List<UnitClassStat> UnitClassStats { get; set; } = new();
    public List<TopStrategyInfo> TopStrategies { get; set; } = new();
    public ModifierInfo? CurrentModifier { get; set; }
}

public class UnitClassStat
{
    public string ClassName { get; set; } = string.Empty;
    public int TimesUsed { get; set; }
    public int Wins { get; set; }
    public double WinRate { get; set; }
    public double UsageRate { get; set; }
    public string Status { get; set; } = "NoData"; // Balanced, Watch, OP, UP, NoData
}

public class TopStrategyInfo
{
    public string Name { get; set; } = string.Empty;
    public string CreatorName { get; set; } = string.Empty;
    public int Downloads { get; set; }
    public double Rating { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ModifierInfo
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

// Guild Analytics
public class AdminGuildAnalyticsData
{
    public int TotalGuilds { get; set; }
    public int TotalGuildMembers { get; set; }
    public double AvgMembersPerGuild { get; set; }
    public int TotalBossesSpawned { get; set; }
    public int TotalBossesDefeated { get; set; }
    public double BossCompletionRate { get; set; }
    public List<AdminGuildSummary> TopGuilds { get; set; } = new();
}

public class AdminGuildSummary
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Tag { get; set; } = string.Empty;
    public int Level { get; set; }
    public int MemberCount { get; set; }
    public int MaxMembers { get; set; }
    public int TreasuryBalance { get; set; }
    public int AvgMemberRating { get; set; }
    public double PremiumRate { get; set; }
    public DateTime CreatedAt { get; set; }
}

// Logs
public class AdminLogData
{
    public List<AdminLogSummary> Logs { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

public class AdminLogSummary
{
    public Guid Id { get; set; }
    public string Level { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? SupportId { get; set; }
    public string? PlayerUsername { get; set; }
    public Guid? PlayerId { get; set; }
    public DateTime CreatedAt { get; set; }
}

// Technical
public class AdminTechnicalData
{
    public int QueuedBattles { get; set; }
    public int MatchedBattles { get; set; }
    public int CompletedBattlesToday { get; set; }
    public int TotalBattles { get; set; }
    public int CompletedBattles { get; set; }
    public int TotalPlayers { get; set; }
    public int TotalUnits { get; set; }
    public int TotalTeams { get; set; }
    public int TotalGuilds { get; set; }
    public int TotalStrategies { get; set; }
    public int TotalChallenges { get; set; }
    public int TotalAchievementUnlocks { get; set; }
}
