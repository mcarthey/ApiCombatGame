namespace ApiCombatGame.Models.DTOs.Premium;

/// <summary>All active perks for the player's current subscription tier.</summary>
public class PremiumPerksResponse
{
    public string CurrentTier { get; set; } = "Free";
    public decimal GoldMultiplier { get; set; } = 1.0m;
    public decimal XpMultiplier { get; set; } = 1.0m;
    public decimal BattlePassXpBonus { get; set; } = 0m;
    public int DailyBattleLimit { get; set; } = 10;
    public int MaxTeamSlots { get; set; } = 3;
    public bool CanCreateGuild { get; set; }
    public bool CanRefreshChallenges { get; set; }
    public int GuaranteedLootEveryNBattles { get; set; }
    public bool HasPremiumBattlePassTrack { get; set; }
    public int MonthlyGemStipend { get; set; }
    public bool HasCreatorBadge { get; set; }
    public bool HasExclusiveCosmetics { get; set; }
    public double LootRarityBoost { get; set; }
    public string? Badge { get; set; }
    public DateTime? NextGemStipendAvailable { get; set; }
}

/// <summary>Response after claiming monthly gem stipend.</summary>
public class GemStipendClaimResponse
{
    public int GemsAwarded { get; set; }
    public int TotalGems { get; set; }
    public DateTime NextClaimAvailable { get; set; }
}

/// <summary>Response after setting a badge.</summary>
public class BadgeResponse
{
    public string? Badge { get; set; }
    public string Message { get; set; } = string.Empty;
}
