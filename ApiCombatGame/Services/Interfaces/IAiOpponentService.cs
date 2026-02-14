using ApiCombatGame.Models.DTOs.AI;
using ApiCombatGame.Models.DTOs.Battle;

namespace ApiCombatGame.Services.Interfaces;

public interface IAiOpponentService
{
    AiOpponentListResponse GetAvailableOpponents();
    Task<BattleResultResponse> FightAiOpponentAsync(Guid playerId, PracticeBattleRequest request);
}
