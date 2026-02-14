namespace ApiCombatGame.Models.Domain;

/// <summary>
/// A seasonal battle pass with 30 levels of rewards across free and premium tracks.
/// Tied to the active ranked season.
/// </summary>
public class BattlePass
{
    public Guid Id { get; set; }
    public Guid SeasonId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int MaxLevel { get; set; } = 30;

    /// <summary>XP required per level (flat per level for simplicity).</summary>
    public int XpPerLevel { get; set; } = 1000;

    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public bool IsActive { get; set; }

    /// <summary>JSON array of level rewards: [{level, track, rewardType, rewardValue, description}]</summary>
    public string RewardsJson { get; set; } = "[]";

    public Season Season { get; set; } = null!;
    public List<PlayerBattlePass> PlayerProgress { get; set; } = new();
}
