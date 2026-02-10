using ApiCombatGame.Models.Domain;

namespace ApiCombatGame.Services.Interfaces;

public interface IReplayService
{
    Task<BattleReplay> CreateReplay(Guid battleId);
    Task<BattleReplay?> GetReplay(string shareUrl);
    Task IncrementViewCount(Guid replayId);
}
