using ApiCombatGame.Models.Domain;
using ApiCombatGame.Models.DTOs.Battle;

namespace ApiCombatGame.Services.Interfaces;

public interface IBattleService
{
    Task<BattleStatusResponse> QueueBattleAsync(Guid playerId, BattleQueueRequest request);
    Task<BattleStatusResponse> GetBattleStatusAsync(Guid battleId, Guid playerId);
    Task<BattleResultResponse> GetBattleResultAsync(Guid battleId, Guid playerId);
    Task<List<BattleResultResponse>> GetBattleHistoryAsync(Guid playerId, int limit, int offset);
    Task ProcessQueuedBattlesAsync(CancellationToken cancellationToken);
}
