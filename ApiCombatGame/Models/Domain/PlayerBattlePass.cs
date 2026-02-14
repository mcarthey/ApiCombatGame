namespace ApiCombatGame.Models.Domain;

/// <summary>
/// Tracks an individual player's battle pass progress for a season.
/// </summary>
public class PlayerBattlePass
{
    public Guid Id { get; set; }
    public Guid PlayerId { get; set; }
    public Guid BattlePassId { get; set; }

    public int CurrentLevel { get; set; } = 0;
    public int CurrentXp { get; set; } = 0;

    /// <summary>Whether the player has purchased the premium track.</summary>
    public bool HasPremium { get; set; }

    /// <summary>JSON array of claimed level numbers, e.g. [1,2,3]</summary>
    public string ClaimedLevelsJson { get; set; } = "[]";

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    public Player Player { get; set; } = null!;
    public BattlePass BattlePass { get; set; } = null!;
}
