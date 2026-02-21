using ApiCombatGame.Models.Domain;

namespace ApiCombatGame.Services.Interfaces;

public class ApiKeyValidationResult
{
    public Guid PlayerId { get; set; }
    public Guid ApiKeyId { get; set; }
}

public interface IApiKeyService
{
    Task<(ApiKey key, string plainTextKey)> CreateApiKeyAsync(Guid playerId, string name);
    Task<List<ApiKey>> GetApiKeysAsync(Guid playerId);
    Task RevokeApiKeyAsync(Guid playerId, Guid apiKeyId);
    Task<ApiKeyValidationResult?> ValidateApiKeyAsync(string plainTextKey);
}
