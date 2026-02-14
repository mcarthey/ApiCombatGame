using ApiCombatGame.Models.Domain;

namespace ApiCombatGame.Services.Interfaces;

public interface IChallengeService
{
    Task<List<DailyChallenge>> GetActiveChallenges(Guid playerId);
    Task GenerateDailyChallenges(Guid playerId);
    Task CheckChallengeProgress(Guid playerId, Battle battle);
    Task ClaimReward(Guid challengeId, Guid playerId);
    Task<bool> RefreshChallengesAsync(Guid playerId);
}
