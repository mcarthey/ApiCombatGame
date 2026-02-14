using ApiCombatGame.Models.DTOs.GuildWar;

namespace ApiCombatGame.Services.Interfaces;

public interface IGuildWarService
{
    Task<GuildWarStatusResponse> GetCurrentWarAsync(Guid playerId);
    Task<List<GuildWarHistoryResponse>> GetWarHistoryAsync(Guid playerId, int limit);
    Task RecordWarContributionAsync(Guid playerId, Guid battleId);
    Task MatchGuildsForWarAsync();
    Task FinalizeExpiredWarsAsync();
}
