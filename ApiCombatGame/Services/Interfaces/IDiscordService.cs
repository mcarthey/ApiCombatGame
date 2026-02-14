using ApiCombatGame.Models.DTOs.Discord;

namespace ApiCombatGame.Services.Interfaces;

public interface IDiscordService
{
    Task<DiscordLinkResponse> LinkAccountAsync(Guid playerId, LinkDiscordRequest request);
    Task<DiscordLinkResponse> VerifyLinkAsync(Guid playerId, string verificationCode);
    Task<DiscordLinkResponse?> GetLinkAsync(Guid playerId);
    Task UnlinkAsync(Guid playerId);
    Task<WebhookResponse> RegisterWebhookAsync(Guid playerId, RegisterWebhookRequest request);
    Task<List<WebhookResponse>> GetWebhooksAsync(Guid playerId);
    Task DeleteWebhookAsync(Guid playerId, Guid webhookId);
    Task<DiscordProfileResponse?> GetProfileByDiscordIdAsync(string discordUserId);
}
