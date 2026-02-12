using System.ComponentModel.DataAnnotations;
using ApiCombatGame.Models.Enums;

namespace ApiCombatGame.Models.Domain;

public class Battle
{
    [Key]
    public Guid Id { get; set; }

    public Guid Player1Id { get; set; }
    public Guid? Player2Id { get; set; }

    public Guid Team1Id { get; set; }
    public Guid? Team2Id { get; set; }

    public BattleStatus Status { get; set; } = BattleStatus.Queued;
    public Guid? WinnerId { get; set; }
    public int Turns { get; set; }

    /// <summary>
    /// JSON-serialized list of battle log entries.
    /// </summary>
    public string BattleLogJson { get; set; } = "[]";

    public string Mode { get; set; } = "ranked";

    /// <summary>JSON array of unit class names for Team 1 (e.g. ["Warrior","Mage"]). Used for challenge checking.</summary>
    public string Team1ClassesJson { get; set; } = "[]";

    /// <summary>JSON array of unit class names for Team 2. Used for challenge checking.</summary>
    public string Team2ClassesJson { get; set; } = "[]";

    public int? Player1RatingChange { get; set; }
    public int? Player2RatingChange { get; set; }
    public int? CurrencyReward { get; set; }

    public DateTime QueuedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    // Navigation
    public Player Player1 { get; set; } = null!;
    public Player? Player2 { get; set; }
}
