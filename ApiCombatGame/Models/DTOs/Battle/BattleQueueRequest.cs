using System.ComponentModel.DataAnnotations;

namespace ApiCombatGame.Models.DTOs.Battle;

/// <summary>Request to enter the battle matchmaking queue.</summary>
public class BattleQueueRequest
{
    /// <summary>The team to fight with. Optional — if omitted, your most recently updated team is used.</summary>
    public Guid TeamId { get; set; }

    /// <summary>Battle mode: "ranked" (affects API rating) or "casual" (no rating change). Default: "ranked".</summary>
    public string Mode { get; set; } = "ranked";
}
