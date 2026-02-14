using System.ComponentModel.DataAnnotations;

namespace ApiCombatGame.Models.DTOs.AI;

/// <summary>Request to fight an AI opponent in a practice battle.</summary>
public class PracticeBattleRequest
{
    /// <summary>Your team ID. Must be a team you own with at least one unit.</summary>
    [Required]
    public Guid TeamId { get; set; }

    /// <summary>AI opponent ID from the GET /api/v1/ai/opponents list (e.g. "novice-1", "expert-3").</summary>
    [Required]
    public string OpponentId { get; set; } = string.Empty;
}
