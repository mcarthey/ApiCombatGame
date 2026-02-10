using System.ComponentModel.DataAnnotations;

namespace ApiCombatGame.Models.DTOs.Battle;

public class BattleQueueRequest
{
    [Required]
    public Guid TeamId { get; set; }

    public string Mode { get; set; } = "ranked";
}
