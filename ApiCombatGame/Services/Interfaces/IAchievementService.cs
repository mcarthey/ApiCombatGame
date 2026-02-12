using ApiCombatGame.Models.Domain;

namespace ApiCombatGame.Services.Interfaces;

public interface IAchievementService
{
    Task<List<PlayerAchievement>> GetPlayerAchievementsAsync(Guid playerId);
    Task CheckAndAwardAsync(Guid playerId, string eventType, Dictionary<string, object>? context = null);
}
