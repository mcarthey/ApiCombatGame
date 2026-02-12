namespace ApiCombatGame.Models.DTOs.Progression;

/// <summary>An achievement with progress tracking and unlock status.</summary>
public class AchievementResponse
{
    /// <summary>Unique achievement identifier.</summary>
    public Guid AchievementId { get; set; }

    /// <summary>Achievement title (e.g., "First Blood", "Strategist Supreme").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>What you need to do to unlock this achievement.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>URL to the achievement's badge icon.</summary>
    public string IconUrl { get; set; } = string.Empty;

    /// <summary>Achievement category (e.g., "Combat", "Collection", "Social").</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>Point value contributing to your achievement score.</summary>
    public int Points { get; set; }

    /// <summary>Current progress toward unlocking.</summary>
    public int Progress { get; set; }

    /// <summary>Progress needed to unlock the achievement.</summary>
    public int RequiredProgress { get; set; }

    /// <summary>Whether you have earned this achievement.</summary>
    public bool IsUnlocked { get; set; }

    /// <summary>Secret achievements have hidden descriptions until unlocked.</summary>
    public bool IsSecret { get; set; }

    /// <summary>UTC timestamp when you unlocked this achievement. Null if locked.</summary>
    public DateTime? UnlockedAt { get; set; }
}

/// <summary>Battle replay data with shareable link and viewer statistics.</summary>
public class ReplayResponse
{
    /// <summary>Unique replay identifier.</summary>
    public Guid ReplayId { get; set; }

    /// <summary>The battle this replay captures.</summary>
    public Guid BattleId { get; set; }

    /// <summary>Unique shareable URL identifier. Append to /api/v1/replays/ to view.</summary>
    public string ShareUrl { get; set; } = string.Empty;

    /// <summary>Number of times this replay has been viewed.</summary>
    public int ViewCount { get; set; }

    /// <summary>Whether this replay has been featured for exceptional gameplay.</summary>
    public bool IsFeatured { get; set; }

    /// <summary>When this replay was created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Username of Player 1 in the battle.</summary>
    public string Player1Name { get; set; } = string.Empty;

    /// <summary>Username of Player 2 in the battle.</summary>
    public string Player2Name { get; set; } = string.Empty;
}

/// <summary>Request to generate a shareable replay from a completed battle.</summary>
public class CreateReplayRequest
{
    /// <summary>The completed battle ID to create a replay for. You must be a participant.</summary>
    public Guid BattleId { get; set; }
}
