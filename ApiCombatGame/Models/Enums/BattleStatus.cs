namespace ApiCombatGame.Models.Enums;

/// <summary>
/// Lifecycle state of a battle from creation to completion.
/// </summary>
public enum BattleStatus
{
    /// <summary>Waiting in the matchmaking queue for an opponent.</summary>
    Queued,
    /// <summary>Battle is actively being simulated by the battle processor.</summary>
    InProgress,
    /// <summary>Battle has finished. Results and rewards are available.</summary>
    Completed,
    /// <summary>Battle was cancelled before completion (e.g., timeout or player disconnect).</summary>
    Cancelled
}
