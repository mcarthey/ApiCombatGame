namespace ApiCombatGame.Models.ViewModels;

public class PlayerDashboardViewModel
{
    // Player basics
    public string Username { get; set; } = string.Empty;
    public int Level { get; set; }
    public int ExperiencePoints { get; set; }
    public int XpToNextLevel { get; set; }
    public int XpProgressPercent { get; set; }
    public int Currency { get; set; }
    public int Rating { get; set; }
    public string Tier { get; set; } = "Free";

    // Login streak
    public int LoginStreak { get; set; }
    public bool TodayRewardClaimed { get; set; }
    public int TodayRewardAmount { get; set; }
    public List<LoginStreakDay> StreakDays { get; set; } = new();

    // Achievements
    public int AchievementPoints { get; set; }
    public int AchievementsUnlocked { get; set; }
    public int TotalAchievements { get; set; }
    public List<AchievementDisplayInfo> RecentAchievements { get; set; } = new();
    public AchievementDisplayInfo? NextAchievement { get; set; }

    // Hero roster
    public List<HeroDisplayInfo> Heroes { get; set; } = new();
    public int TotalUnitsAvailable { get; set; }
    public int AffordableUnlockCount { get; set; }

    // Daily challenges
    public List<ChallengeDisplayInfo> Challenges { get; set; } = new();

    // Guild
    public GuildDisplayInfo? Guild { get; set; }
    public GuildBossDisplayInfo? GuildBoss { get; set; }

    // Battle status
    public int BattlesToday { get; set; }
    public int DailyBattleLimit { get; set; } // -1 = unlimited
    public int WinStreak { get; set; }
    public bool HasBattledToday { get; set; }

    // Notifications
    public int UnreadNotifications { get; set; }

    // Suggested actions
    public List<SuggestedAction> SuggestedActions { get; set; } = new();
}

public class LoginStreakDay
{
    public int Day { get; set; }
    public int RewardAmount { get; set; }
    public bool IsClaimed { get; set; }
    public bool IsToday { get; set; }
}

public class AchievementDisplayInfo
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string IconUrl { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int Points { get; set; }
    public int Progress { get; set; }
    public int RequiredProgress { get; set; }
    public int ProgressPercent { get; set; }
    public bool IsUnlocked { get; set; }
    public DateTime? UnlockedAt { get; set; }
}

public class HeroDisplayInfo
{
    public Guid UnitId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public int MasteryLevel { get; set; }
    public int Attack { get; set; }
    public int Defense { get; set; }
    public int Speed { get; set; }
    public int Health { get; set; }
    public bool IsHighestMastery { get; set; }
}

public class ChallengeDisplayInfo
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Progress { get; set; }
    public int RequiredProgress { get; set; }
    public int ProgressPercent { get; set; }
    public bool IsCompleted { get; set; }
    public int RewardCurrency { get; set; }
    public int RewardXp { get; set; }
    public string TimeRemaining { get; set; } = string.Empty;
}

public class GuildDisplayInfo
{
    public Guid GuildId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Tag { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int ContributionPoints { get; set; }
    public int MemberCount { get; set; }
}

public class GuildBossDisplayInfo
{
    public string Name { get; set; } = string.Empty;
    public int CurrentHp { get; set; }
    public int MaxHp { get; set; }
    public int HpPercent { get; set; }
    public bool IsDefeated { get; set; }
}

public class SuggestedAction
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string LinkUrl { get; set; } = string.Empty;
    public string LinkText { get; set; } = string.Empty;
    public string IconType { get; set; } = string.Empty; // sword, shield, trophy, guild, star, coin
}
