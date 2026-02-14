using ApiCombatGame.Models.DTOs.Rival;

namespace ApiCombatGame.Services.Interfaces;

public interface IRivalService
{
    Task<RivalInfoResponse> GetRivalAsync(Guid playerId);
    Task CheckRivalBattleAsync(Guid winnerId, Guid loserId, Guid battleId);
}
