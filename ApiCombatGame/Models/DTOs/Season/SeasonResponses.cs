namespace ApiCombatGame.Models.DTOs.Season;

/// <summary>Information about the current ranked season.</summary>
public class CurrentSeasonResponse
{
    /// <summary>Unique season identifier.</summary>
    public Guid SeasonId { get; set; }

    /// <summary>Display name of the season (e.g. "Season 1: Dawn of Battle").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Season number (1, 2, 3...).</summary>
    public int SeasonNumber { get; set; }

    /// <summary>UTC start date of this season.</summary>
    public DateTime StartDate { get; set; }

    /// <summary>UTC end date of this season.</summary>
    public DateTime EndDate { get; set; }

    /// <summary>Days remaining in this season.</summary>
    public int DaysRemaining { get; set; }

    /// <summary>Your current ranked tier this season (Bronze through Legend).</summary>
    public string CurrentTier { get; set; } = string.Empty;

    /// <summary>Your current season API rating.</summary>
    public int SeasonRating { get; set; }

    /// <summary>Your peak rating achieved this season.</summary>
    public int PeakRating { get; set; }

    /// <summary>Your highest tier achieved this season (for end-of-season rewards).</summary>
    public string PeakTier { get; set; } = string.Empty;

    /// <summary>Wins this season.</summary>
    public int Wins { get; set; }

    /// <summary>Losses this season.</summary>
    public int Losses { get; set; }

    /// <summary>Draws this season.</summary>
    public int Draws { get; set; }

    /// <summary>Win rate percentage this season.</summary>
    public decimal WinRate { get; set; }

    /// <summary>Rating needed to reach the next tier. Null if already Legend.</summary>
    public int? RatingToNextTier { get; set; }

    /// <summary>Name of the next tier. Null if already Legend.</summary>
    public string? NextTier { get; set; }

    /// <summary>Rating thresholds for each tier this season.</summary>
    public Dictionary<string, int> TierThresholds { get; set; } = new();
}

/// <summary>A player's ranking in the seasonal leaderboard.</summary>
public class SeasonLeaderboardEntry
{
    /// <summary>Leaderboard position (1-based).</summary>
    public int Rank { get; set; }

    /// <summary>Player ID.</summary>
    public Guid PlayerId { get; set; }

    /// <summary>Player display name.</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>Current season tier.</summary>
    public string Tier { get; set; } = string.Empty;

    /// <summary>Current season API rating.</summary>
    public int Rating { get; set; }

    /// <summary>Total wins this season.</summary>
    public int Wins { get; set; }

    /// <summary>Total losses this season.</summary>
    public int Losses { get; set; }

    /// <summary>Win rate percentage.</summary>
    public decimal WinRate { get; set; }
}

/// <summary>Response containing the seasonal leaderboard.</summary>
public class SeasonLeaderboardResponse
{
    /// <summary>Season name.</summary>
    public string SeasonName { get; set; } = string.Empty;

    /// <summary>Ranked players ordered by rating.</summary>
    public List<SeasonLeaderboardEntry> Rankings { get; set; } = new();

    /// <summary>Total players ranked this season.</summary>
    public int TotalPlayers { get; set; }

    /// <summary>Your position on the leaderboard. Null if not ranked.</summary>
    public int? YourRank { get; set; }
}

/// <summary>Rewards earned at the end of a season based on peak tier achieved.</summary>
public class SeasonRewardsResponse
{
    /// <summary>Season this reward is from.</summary>
    public string SeasonName { get; set; } = string.Empty;

    /// <summary>Peak tier achieved during this season.</summary>
    public string PeakTier { get; set; } = string.Empty;

    /// <summary>Gold reward earned.</summary>
    public int GoldReward { get; set; }

    /// <summary>XP reward earned.</summary>
    public int XpReward { get; set; }

    /// <summary>Exclusive title earned (if any).</summary>
    public string? ExclusiveTitle { get; set; }

    /// <summary>Whether rewards were successfully claimed.</summary>
    public bool Claimed { get; set; }
}
