using ApiCombatGame.Models.Domain;
using ApiCombatGame.Models.DTOs.Strategy;

namespace ApiCombatGame.Services.Interfaces;

public interface IStrategyEngine
{
    /// <summary>
    /// Resolves a full battle between two teams using their strategies.
    /// Returns the battle log and winner information.
    /// </summary>
    BattleResolution ResolveBattle(
        List<Unit> team1Units,
        StrategyConfig team1Strategy,
        List<Unit> team2Units,
        StrategyConfig team2Strategy,
        int maxTurns = 50,
        int? seed = null);
}

public class BattleResolution
{
    public int WinnerTeam { get; set; } // 1 or 2, 0 for draw
    public int TotalTurns { get; set; }
    public List<BattleLogEntry> Log { get; set; } = new();
}
