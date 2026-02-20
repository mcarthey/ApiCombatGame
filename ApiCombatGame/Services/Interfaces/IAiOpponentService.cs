using ApiCombatGame.Models.DTOs.AI;
using ApiCombatGame.Models.DTOs.Battle;
using ApiCombatGame.Models.DTOs.Education;

namespace ApiCombatGame.Services.Interfaces;

public interface IAiOpponentService
{
    AiOpponentListResponse GetAvailableOpponents();
    Task<BattleResultResponse> FightAiOpponentAsync(Guid playerId, PracticeBattleRequest request);
    Task<BatchPracticeResponse> BatchPracticeAsync(Guid playerId, BatchPracticeRequest request);
}
