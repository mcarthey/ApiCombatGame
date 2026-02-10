using ApiCombatGame.Models.DTOs.Strategy;

namespace ApiCombatGame.Models.DTOs.Team;

public class TeamResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<UnitSummary> Units { get; set; } = new();
    public StrategyConfig? Strategy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class UnitSummary
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Class { get; set; } = string.Empty;
    public int Level { get; set; }
    public int Health { get; set; }
    public int Attack { get; set; }
    public int Defense { get; set; }
    public int Speed { get; set; }
}
