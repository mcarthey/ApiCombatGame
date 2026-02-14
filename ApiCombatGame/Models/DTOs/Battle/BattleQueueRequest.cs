using System.ComponentModel.DataAnnotations;

namespace ApiCombatGame.Models.DTOs.Battle;

/// <summary>Request to enter the battle matchmaking queue.</summary>
public class BattleQueueRequest
{
    /// <summary>The team to fight with. Must be a team you own with at least one unit.</summary>
    [Required]
    public Guid TeamId { get; set; }

    /// <summary>Battle mode: "ranked" (affects API rating) or "casual" (no rating change). Default: "ranked".</summary>
    public string Mode { get; set; } = "ranked";
}
