using ApiCombatGame.Models.DTOs.Activity;

namespace ApiCombatGame.Services.Interfaces;

public interface IActivityFeedService
{
    Task LogActivityAsync(Guid playerId, string activityType, string description,
        Guid? relatedEntityId = null, string? metadataJson = null, bool isPublic = true);
    Task<ActivityFeedResponse> GetFeedAsync(Guid playerId, int page = 1, int pageSize = 20);
    Task<ActivityFeedResponse> GetPublicFeedAsync(string username, int page = 1, int pageSize = 20);
    Task<PlayerLifetimeStatsResponse> GetLifetimeStatsAsync(Guid playerId);
    Task<PlayerLifetimeStatsResponse> GetPublicStatsAsync(string username);
    Task RecordBattleStatsAsync(Guid playerId, bool won, int goldEarned, int xpEarned, int newRating);
}
