using ApiCombatGame.Models.Domain;

namespace ApiCombatGame.Services.Interfaces;

public interface IApiKeyService
{
    Task<(ApiKey key, string plainTextKey)> CreateApiKeyAsync(Guid playerId, string name);
    Task<List<ApiKey>> GetApiKeysAsync(Guid playerId);
    Task RevokeApiKeyAsync(Guid playerId, Guid apiKeyId);
    Task<Guid?> ValidateApiKeyAsync(string plainTextKey);
}
