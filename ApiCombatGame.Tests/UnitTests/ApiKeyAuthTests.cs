using ApiCombatGame.Data;
using ApiCombatGame.Models.Domain;
using ApiCombatGame.Services;
using ApiCombatGame.Services.Interfaces;
using ApiCombatGame.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ApiCombatGame.Tests.UnitTests;

public class ApiKeyAuthTests : IDisposable
{
    private readonly GameDbContext _context;
    private readonly ApiKeyService _service;

    public ApiKeyAuthTests()
    {
        _context = TestDbContextFactory.Create();
        var logger = NullLogger<ApiKeyService>.Instance;
        _service = new ApiKeyService(_context, logger);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task ValidateApiKeyAsync_NullOrEmpty_ReturnsNull()
    {
        Assert.Null(await _service.ValidateApiKeyAsync(null!));
        Assert.Null(await _service.ValidateApiKeyAsync(""));
        Assert.Null(await _service.ValidateApiKeyAsync("   "));
    }

    [Fact]
    public async Task ValidateApiKeyAsync_WrongPrefix_ReturnsNull()
    {
        // Keys must start with "acg_"
        Assert.Null(await _service.ValidateApiKeyAsync("bad_prefix_key_here"));
        Assert.Null(await _service.ValidateApiKeyAsync("Bearer some.jwt.token"));
    }

    [Fact]
    public async Task ValidateApiKeyAsync_NonexistentKey_ReturnsNull()
    {
        Assert.Null(await _service.ValidateApiKeyAsync("acg_AAAAAAAABBBBBBBBCCCCCCCC"));
    }

    [Fact]
    public async Task ValidateApiKeyAsync_ValidKey_ReturnsPlayerAndKeyIds()
    {
        var player = TestDbContextFactory.CreatePlayer(_context, "apikey_user");
        var (key, plainTextKey) = await _service.CreateApiKeyAsync(player.Id, "Test Key");

        var result = await _service.ValidateApiKeyAsync(plainTextKey);

        Assert.NotNull(result);
        Assert.Equal(player.Id, result.PlayerId);
        Assert.Equal(key.Id, result.ApiKeyId);
    }

    [Fact]
    public async Task ValidateApiKeyAsync_RevokedKey_ReturnsNull()
    {
        var player = TestDbContextFactory.CreatePlayer(_context, "revoked_user");
        var (key, plainTextKey) = await _service.CreateApiKeyAsync(player.Id, "Revoke Me");

        await _service.RevokeApiKeyAsync(player.Id, key.Id);

        var result = await _service.ValidateApiKeyAsync(plainTextKey);
        Assert.Null(result);
    }

    [Fact]
    public async Task ValidateApiKeyAsync_UpdatesLastUsedAt()
    {
        var player = TestDbContextFactory.CreatePlayer(_context, "lastused_user");
        var (key, plainTextKey) = await _service.CreateApiKeyAsync(player.Id, "Track Usage");

        Assert.Null(key.LastUsedAt);

        await _service.ValidateApiKeyAsync(plainTextKey);

        var updated = await _context.ApiKeys.FindAsync(key.Id);
        Assert.NotNull(updated!.LastUsedAt);
    }

    [Fact]
    public async Task CreateApiKeyAsync_EnforcesMaxFiveKeys()
    {
        var player = TestDbContextFactory.CreatePlayer(_context, "maxkeys_user");

        for (int i = 0; i < 5; i++)
            await _service.CreateApiKeyAsync(player.Id, $"Key {i}");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateApiKeyAsync(player.Id, "Key 6"));
    }

    [Fact]
    public async Task CreateApiKeyAsync_KeyStartsWithAcgPrefix()
    {
        var player = TestDbContextFactory.CreatePlayer(_context, "prefix_user");
        var (_, plainTextKey) = await _service.CreateApiKeyAsync(player.Id, "Prefix Check");

        Assert.StartsWith("acg_", plainTextKey);
    }

    [Fact]
    public async Task RevokeApiKeyAsync_SetsInactiveAndRevokedAt()
    {
        var player = TestDbContextFactory.CreatePlayer(_context, "revoker");
        var (key, _) = await _service.CreateApiKeyAsync(player.Id, "To Revoke");

        Assert.True(key.IsActive);
        Assert.Null(key.RevokedAt);

        await _service.RevokeApiKeyAsync(player.Id, key.Id);

        var revoked = await _context.ApiKeys.FindAsync(key.Id);
        Assert.False(revoked!.IsActive);
        Assert.NotNull(revoked.RevokedAt);
    }

    [Fact]
    public async Task RevokeApiKeyAsync_WrongPlayer_Throws()
    {
        var player1 = TestDbContextFactory.CreatePlayer(_context, "owner");
        var player2 = TestDbContextFactory.CreatePlayer(_context, "other");
        var (key, _) = await _service.CreateApiKeyAsync(player1.Id, "My Key");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.RevokeApiKeyAsync(player2.Id, key.Id));
    }
}
