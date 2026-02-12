using ApiCombatGame.Models.Domain;

namespace ApiCombatGame.Services.Interfaces;

public interface IGuildChatService
{
    Task<List<GuildChatMessage>> GetMessagesAsync(Guid guildId, int limit = 50, Guid? beforeId = null);
    Task<GuildChatMessage> PostMessageAsync(Guid guildId, Guid playerId, string message);
    Task PostSystemMessageAsync(Guid guildId, string message);
}
