namespace ApiCombatGame.Models.DTOs.Marketplace;

/// <summary>A strategy listing in the marketplace.</summary>
public class StrategyResponse
{
    /// <summary>Unique strategy identifier.</summary>
    public Guid StrategyId { get; set; }

    /// <summary>Strategy display name chosen by the creator.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Creator's description of the strategy and its intended playstyle.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Username of the player who published this strategy.</summary>
    public string CreatorName { get; set; } = string.Empty;

    /// <summary>Cost in currency to purchase. 0 means free.</summary>
    public int Price { get; set; }

    /// <summary>Number of times this strategy has been downloaded.</summary>
    public int DownloadCount { get; set; }

    /// <summary>Community rating average (1.0-5.0 scale).</summary>
    public double AverageRating { get; set; }

    /// <summary>Historical win rate percentage of battles using this strategy (0-100).</summary>
    public double WinRate { get; set; }

    /// <summary>Anti-meta decay multiplier (0.5-1.0). Strategies lose effectiveness over time to prevent stagnation.</summary>
    public double EffectivenessMultiplier { get; set; }

    /// <summary>When this strategy was published.</summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>Full strategy data returned after purchase, including the JSON configuration.</summary>
public class StrategyDownloadResponse
{
    /// <summary>Unique strategy identifier.</summary>
    public Guid StrategyId { get; set; }

    /// <summary>Strategy display name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>The full strategy JSON. Use directly as the strategy field in team configuration.</summary>
    public string StrategyJson { get; set; } = "{}";
}
