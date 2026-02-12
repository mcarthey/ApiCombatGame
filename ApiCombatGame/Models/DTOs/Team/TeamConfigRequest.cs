using System.ComponentModel.DataAnnotations;
using ApiCombatGame.Models.DTOs.Strategy;

namespace ApiCombatGame.Models.DTOs.Team;

/// <summary>Configuration for creating or updating a team of combat units.</summary>
public class TeamConfigRequest
{
    /// <summary>Display name for this team. 1-100 characters.</summary>
    [Required]
    [MinLength(1)]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>IDs of units from your roster to include. 1-5 units required.</summary>
    [Required]
    [MinLength(1)]
    [MaxLength(5)]
    public List<Guid> UnitIds { get; set; } = new();

    /// <summary>Optional battle strategy controlling unit AI behavior. If null, units use default targeting.</summary>
    public StrategyConfig? Strategy { get; set; }
}
