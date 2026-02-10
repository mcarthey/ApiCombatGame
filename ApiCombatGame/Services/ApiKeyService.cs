using System.Security.Cryptography;
using ApiCombatGame.Data;
using ApiCombatGame.Models.Domain;
using ApiCombatGame.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ApiCombatGame.Services;

public class ApiKeyService : IApiKeyService
{
    private readonly GameDbContext _context;
    private readonly ILogger<ApiKeyService> _logger;

    public ApiKeyService(GameDbContext context, ILogger<ApiKeyService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<(ApiKey key, string plainTextKey)> CreateApiKeyAsync(Guid playerId, string name)
    {
        var player = await _context.Players.FindAsync(playerId)
            ?? throw new InvalidOperationException("Player not found.");

        // Check max API keys (limit to 5)
        var existingCount = await _context.ApiKeys.CountAsync(k => k.PlayerId == playerId && k.IsActive);
        if (existingCount >= 5)
            throw new InvalidOperationException("Maximum of 5 active API keys allowed.");

        // Generate a secure API key
        var rawBytes = RandomNumberGenerator.GetBytes(32);
        var rawKey = Convert.ToBase64String(rawBytes).Replace("+", "").Replace("/", "").Replace("=", "");
        var plainTextKey = $"acg_{rawKey}";
        var prefix = plainTextKey[..12];
        var hash = BCrypt.Net.BCrypt.HashPassword(plainTextKey);

        var apiKey = new ApiKey
        {
            Id = Guid.NewGuid(),
            PlayerId = playerId,
            Name = name,
            KeyHash = hash,
            KeyPrefix = prefix,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.ApiKeys.Add(apiKey);
        await _context.SaveChangesAsync();

        _logger.LogInformation("API key created for player {PlayerId}: {KeyPrefix}...", playerId, prefix);

        return (apiKey, plainTextKey);
    }

    public async Task<List<ApiKey>> GetApiKeysAsync(Guid playerId)
    {
        return await _context.ApiKeys
            .Where(k => k.PlayerId == playerId && k.IsActive)
            .OrderByDescending(k => k.CreatedAt)
            .ToListAsync();
    }

    public async Task RevokeApiKeyAsync(Guid playerId, Guid apiKeyId)
    {
        var apiKey = await _context.ApiKeys
            .FirstOrDefaultAsync(k => k.Id == apiKeyId && k.PlayerId == playerId);

        if (apiKey == null)
            throw new InvalidOperationException("API key not found.");

        apiKey.IsActive = false;
        apiKey.RevokedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("API key revoked for player {PlayerId}: {KeyPrefix}...", playerId, apiKey.KeyPrefix);
    }

    public async Task<Guid?> ValidateApiKeyAsync(string plainTextKey)
    {
        if (string.IsNullOrEmpty(plainTextKey) || !plainTextKey.StartsWith("acg_"))
            return null;

        var prefix = plainTextKey[..12];

        var candidates = await _context.ApiKeys
            .Where(k => k.KeyPrefix == prefix && k.IsActive)
            .ToListAsync();

        foreach (var candidate in candidates)
        {
            if (BCrypt.Net.BCrypt.Verify(plainTextKey, candidate.KeyHash))
            {
                candidate.LastUsedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return candidate.PlayerId;
            }
        }

        return null;
    }
}
