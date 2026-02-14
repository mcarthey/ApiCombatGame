namespace ApiCombatGame.Models.DTOs.BattlePass;

/// <summary>Player's current battle pass progress.</summary>
public class BattlePassProgressResponse
{
    /// <summary>Name of the current battle pass.</summary>
    public string PassName { get; set; } = string.Empty;

    /// <summary>Current level (0-30).</summary>
    public int CurrentLevel { get; set; }

    /// <summary>XP accumulated toward the next level.</summary>
    public int CurrentXp { get; set; }

    /// <summary>XP needed to reach the next level.</summary>
    public int XpToNextLevel { get; set; }

    /// <summary>Maximum level in this pass.</summary>
    public int MaxLevel { get; set; } = 30;

    /// <summary>Overall XP progress as a percentage (0-100).</summary>
    public double OverallProgressPercent { get; set; }

    /// <summary>Whether the player has premium track access.</summary>
    public bool HasPremium { get; set; }

    /// <summary>All rewards with claim status.</summary>
    public List<BattlePassLevelReward> Rewards { get; set; } = new();

    /// <summary>When the pass expires.</summary>
    public DateTime EndsAt { get; set; }

    /// <summary>Days remaining before the pass resets.</summary>
    public int DaysRemaining { get; set; }
}

/// <summary>A single level's reward info.</summary>
public class BattlePassLevelReward
{
    /// <summary>The level number (1-30).</summary>
    public int Level { get; set; }

    /// <summary>"free" or "premium".</summary>
    public string Track { get; set; } = "free";

    /// <summary>Type of reward: "currency", "xp", "title", "loot_box".</summary>
    public string RewardType { get; set; } = string.Empty;

    /// <summary>Numeric value (gold amount, XP amount, etc.).</summary>
    public int RewardValue { get; set; }

    /// <summary>Human-readable description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Whether this reward has been claimed.</summary>
    public bool Claimed { get; set; }

    /// <summary>Whether the player can claim this (level reached + track access).</summary>
    public bool CanClaim { get; set; }
}

/// <summary>Response after claiming a reward.</summary>
public class ClaimRewardResponse
{
    public bool Success { get; set; }
    public int Level { get; set; }
    public string Track { get; set; } = string.Empty;
    public string RewardType { get; set; } = string.Empty;
    public int RewardValue { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

/// <summary>Response after XP is added to the battle pass.</summary>
public class BattlePassXpGainResponse
{
    public int XpGained { get; set; }
    public int NewXp { get; set; }
    public int NewLevel { get; set; }
    public bool LeveledUp { get; set; }
    public int LevelsGained { get; set; }
}
