namespace ApiCombatGame.Models.DTOs.Activity;

/// <summary>A single activity in the feed.</summary>
public class ActivityFeedItem
{
    public Guid Id { get; set; }
    public string ActivityType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid? RelatedEntityId { get; set; }
    public string? MetadataJson { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>Activity feed response with pagination.</summary>
public class ActivityFeedResponse
{
    public string Username { get; set; } = string.Empty;
    public List<ActivityFeedItem> Activities { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

/// <summary>Lifetime stats for a player (sunk cost investment).</summary>
public class PlayerLifetimeStatsResponse
{
    public string Username { get; set; } = string.Empty;
    public int Level { get; set; }
    public int Rating { get; set; }
    public string CurrentTier { get; set; } = "Free";
    public string? Badge { get; set; }

    // Battle stats
    public int TotalBattlesPlayed { get; set; }
    public int TotalBattlesWon { get; set; }
    public int TotalBattlesLost { get; set; }
    public double WinRate { get; set; }
    public int CurrentWinStreak { get; set; }
    public int HighestWinStreak { get; set; }
    public int HighestRating { get; set; }

    // Economy stats
    public long TotalGoldEarned { get; set; }
    public int CurrentGold { get; set; }
    public long TotalXpEarned { get; set; }
    public int GemsEarnedTotal { get; set; }
    public int GemsSpentTotal { get; set; }

    // Collection stats
    public int UnitsUnlocked { get; set; }
    public int TeamsCreated { get; set; }
    public int AchievementsUnlocked { get; set; }
    public int AchievementPoints { get; set; }
    public int CosmeticsOwned { get; set; }

    // Engagement stats
    public int LoginStreak { get; set; }
    public int DaysPlayed { get; set; }
    public DateTime MemberSince { get; set; }

    // Summary
    public string InvestmentSummary { get; set; } = string.Empty;
}
