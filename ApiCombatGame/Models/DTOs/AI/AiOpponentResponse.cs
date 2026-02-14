namespace ApiCombatGame.Models.DTOs.AI;

/// <summary>An AI opponent available for practice battles.</summary>
public class AiOpponentResponse
{
    /// <summary>Unique identifier for this AI opponent preset.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Display name of the AI opponent.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Flavor text describing this opponent's personality and fighting style.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Difficulty level: "novice", "intermediate", or "expert".</summary>
    public string Difficulty { get; set; } = string.Empty;

    /// <summary>Approximate API rating of this AI opponent. Compare against your own to gauge difficulty.</summary>
    public int ApproximateRating { get; set; }

    /// <summary>Number of units in the AI's team.</summary>
    public int TeamSize { get; set; }

    /// <summary>Unit classes in the AI team (e.g. ["Warrior", "Mage", "Healer"]).</summary>
    public List<string> TeamClasses { get; set; } = new();

    /// <summary>The formation the AI uses: "balanced", "aggressive", or "defensive".</summary>
    public string Formation { get; set; } = string.Empty;
}

/// <summary>Full list of available AI opponents grouped by difficulty.</summary>
public class AiOpponentListResponse
{
    /// <summary>Available AI opponents.</summary>
    public List<AiOpponentResponse> Opponents { get; set; } = new();

    /// <summary>Reward modifier for practice battles (0.5 = 50% of normal rewards).</summary>
    public decimal RewardMultiplier { get; set; } = 0.5m;

    /// <summary>Practice battles never affect your API rating.</summary>
    public bool RatingAffected { get; set; } = false;

    /// <summary>Practice battles do not count against your daily battle limit.</summary>
    public bool CountsTowardDailyLimit { get; set; } = false;
}
