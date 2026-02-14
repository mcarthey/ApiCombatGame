using ApiCombatGame.Models.DTOs.BattlePass;

namespace ApiCombatGame.Services.Interfaces;

public interface IBattlePassService
{
    Task<BattlePassProgressResponse> GetProgressAsync(Guid playerId);
    Task<ClaimRewardResponse> ClaimRewardAsync(Guid playerId, int level);
    Task<BattlePassXpGainResponse> AddXpAsync(Guid playerId, int xp, string source);
}
