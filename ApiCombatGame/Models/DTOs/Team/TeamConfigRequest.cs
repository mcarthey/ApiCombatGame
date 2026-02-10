using System.ComponentModel.DataAnnotations;
using ApiCombatGame.Models.DTOs.Strategy;

namespace ApiCombatGame.Models.DTOs.Team;

public class TeamConfigRequest
{
    [Required]
    [MinLength(1)]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MinLength(1)]
    [MaxLength(5)]
    public List<Guid> UnitIds { get; set; } = new();

    public StrategyConfig? Strategy { get; set; }
}
