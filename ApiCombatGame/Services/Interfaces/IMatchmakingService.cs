using ApiCombatGame.Models.Domain;

namespace ApiCombatGame.Services.Interfaces;

public interface IMatchmakingService
{
    /// <summary>
    /// Tries to find a matching opponent for a queued battle.
    /// Returns the matched battle pair, or null if no match found.
    /// </summary>
    Task<(Battle battle1, Battle battle2)?> FindMatchAsync();
}
