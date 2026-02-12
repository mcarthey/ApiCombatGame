using ApiCombatGame.Models.Domain;

namespace ApiCombatGame.Services.Interfaces;

public interface IGuildStrategyService
{
    Task<List<GuildStrategy>> GetStrategiesAsync(Guid guildId);
    Task<GuildStrategy> PublishAsync(Guid guildId, Guid creatorId, string name, string description, string strategyJson);
    Task<GuildStrategy> UpdateAsync(Guid guildId, Guid strategyId, Guid requestingPlayerId, string? name, string? description, string? strategyJson);
    Task DeleteAsync(Guid guildId, Guid strategyId, Guid requestingPlayerId);
}
