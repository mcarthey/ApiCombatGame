namespace ApiCombatGame.Models.DTOs.Battle;

/// <summary>Real-time status of a battle from queue to completion.</summary>
public class BattleStatusResponse
{
    /// <summary>Unique battle identifier. Use this to poll for updates and retrieve results.</summary>
    public Guid BattleId { get; set; }

    /// <summary>Current state: Queued, InProgress, Completed, or Cancelled.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Your position in the matchmaking queue. Null once matched.</summary>
    public int? QueuePosition { get; set; }

    /// <summary>Estimated seconds until a match is found. Null once matched.</summary>
    public int? EstimatedWaitSeconds { get; set; }

    /// <summary>UTC timestamp when the battle was queued.</summary>
    public DateTime QueuedAt { get; set; }

    /// <summary>UTC timestamp when the battle began. Null if still queued.</summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>UTC timestamp when the battle concluded. Null if not yet finished.</summary>
    public DateTime? CompletedAt { get; set; }
}
