using ApiCombatGame.Models.Domain;
using ApiCombatGame.Services;
using ApiCombatGame.Tests.Helpers;
using Xunit;

namespace ApiCombatGame.Tests.UnitTests;

public class ActivityLedgerTests : IDisposable
{
    private readonly Data.GameDbContext _context;
    private readonly ActivityLedger _ledger;

    public ActivityLedgerTests()
    {
        _context = TestDbContextFactory.Create();
        _ledger = new ActivityLedger(_context);
    }

    public void Dispose() => _context.Dispose();

    [Fact]
    public async Task LogPlayer_SkipsWhenOldEqualsNew()
    {
        var playerId = Guid.NewGuid();
        _ledger.LogPlayer(playerId, "Currency", 1000, 1000, "Battle", "BattleWon");
        await _context.SaveChangesAsync();

        var entries = await _ledger.GetPlayerLedgerAsync(playerId);
        Assert.Empty(entries);
    }

    [Fact]
    public async Task LogPlayer_CreatesEntryWithCorrectFields()
    {
        var playerId = Guid.NewGuid();
        var battleId = Guid.NewGuid();

        _ledger.LogPlayer(playerId, "Currency", 1000, 1050, "Battle", "BattleWon", battleId, "50g reward");
        await _context.SaveChangesAsync();

        var entries = await _ledger.GetPlayerLedgerAsync(playerId);
        Assert.Single(entries);

        var entry = entries[0];
        Assert.Equal(playerId, entry.EntityId);
        Assert.Equal("Player", entry.EntityType);
        Assert.Equal("Currency", entry.Property);
        Assert.Equal(1000, entry.OldValue);
        Assert.Equal(1050, entry.NewValue);
        Assert.Equal(50, entry.Delta);
        Assert.Equal("Battle", entry.Source);
        Assert.Equal("BattleWon", entry.Action);
        Assert.Equal(battleId, entry.RelatedEntityId);
        Assert.Equal("50g reward", entry.ContextJson);
    }

    [Fact]
    public async Task LogPlayer_CalculatesDeltaCorrectly()
    {
        var playerId = Guid.NewGuid();

        // Positive delta (gain)
        _ledger.LogPlayer(playerId, "Rating", 1000, 1016, "Battle", "BattleWon");
        // Negative delta (loss)
        _ledger.LogPlayer(playerId, "Currency", 500, 300, "Cosmetic", "CosmeticPurchased");
        await _context.SaveChangesAsync();

        var entries = await _ledger.GetPlayerLedgerAsync(playerId);
        Assert.Equal(2, entries.Count);

        var ratingEntry = entries.First(e => e.Property == "Rating");
        Assert.Equal(16, ratingEntry.Delta);

        var currencyEntry = entries.First(e => e.Property == "Currency");
        Assert.Equal(-200, currencyEntry.Delta);
    }

    [Fact]
    public async Task LogGuild_SetsEntityTypeToGuild()
    {
        var guildId = Guid.NewGuid();
        _ledger.LogGuild(guildId, "TreasuryBalance", 5000, 5500, "GuildBoss", "BossDefeated");
        await _context.SaveChangesAsync();

        var entries = await _ledger.GetGuildLedgerAsync(guildId);
        Assert.Single(entries);
        Assert.Equal("Guild", entries[0].EntityType);
        Assert.Equal(500, entries[0].Delta);
    }

    [Fact]
    public async Task GetPlayerLedgerAsync_ReturnsNewestFirst()
    {
        var playerId = Guid.NewGuid();

        _ledger.LogPlayer(playerId, "Currency", 1000, 1050, "Battle", "BattleWon");
        await _context.SaveChangesAsync();

        // Force a different timestamp
        await Task.Delay(10);
        _ledger.LogPlayer(playerId, "Currency", 1050, 1100, "Battle", "BattleWon");
        await _context.SaveChangesAsync();

        await Task.Delay(10);
        _ledger.LogPlayer(playerId, "Currency", 1100, 1150, "Battle", "BattleWon");
        await _context.SaveChangesAsync();

        var entries = await _ledger.GetPlayerLedgerAsync(playerId);
        Assert.Equal(3, entries.Count);
        Assert.Equal(1150, entries[0].NewValue); // Most recent first
        Assert.Equal(1050, entries[2].NewValue); // Oldest last
    }

    [Fact]
    public async Task GetPlayerLedgerAsync_FiltersByProperty()
    {
        var playerId = Guid.NewGuid();

        _ledger.LogPlayer(playerId, "Currency", 1000, 1050, "Battle", "BattleWon");
        _ledger.LogPlayer(playerId, "Rating", 1000, 1016, "Battle", "BattleWon");
        _ledger.LogPlayer(playerId, "ExperiencePoints", 0, 100, "Battle", "BattleWon");
        await _context.SaveChangesAsync();

        var currencyEntries = await _ledger.GetPlayerLedgerAsync(playerId, property: "Currency");
        Assert.Single(currencyEntries);
        Assert.Equal("Currency", currencyEntries[0].Property);

        var allEntries = await _ledger.GetPlayerLedgerAsync(playerId);
        Assert.Equal(3, allEntries.Count);
    }

    [Fact]
    public async Task GetPlayerLedgerAsync_FiltersBySince()
    {
        var playerId = Guid.NewGuid();
        var cutoff = DateTime.UtcNow;

        // Add entry with a timestamp before cutoff
        _context.LedgerEntries.Add(new LedgerEntry
        {
            Id = Guid.NewGuid(),
            EntityId = playerId,
            EntityType = "Player",
            Property = "Currency",
            OldValue = 1000,
            NewValue = 1050,
            Delta = 50,
            Source = "Battle",
            Action = "BattleWon",
            CreatedAt = cutoff.AddHours(-2)
        });

        // Add entry with a timestamp after cutoff
        _context.LedgerEntries.Add(new LedgerEntry
        {
            Id = Guid.NewGuid(),
            EntityId = playerId,
            EntityType = "Player",
            Property = "Currency",
            OldValue = 1050,
            NewValue = 1100,
            Delta = 50,
            Source = "Battle",
            Action = "BattleWon",
            CreatedAt = cutoff.AddHours(1)
        });
        await _context.SaveChangesAsync();

        var recentEntries = await _ledger.GetPlayerLedgerAsync(playerId, since: cutoff);
        Assert.Single(recentEntries);
        Assert.Equal(1100, recentEntries[0].NewValue);
    }

    [Fact]
    public async Task GetRelatedEntriesAsync_ReturnsCorrectEntries()
    {
        var battleId = Guid.NewGuid();
        var otherBattleId = Guid.NewGuid();
        var player1 = Guid.NewGuid();
        var player2 = Guid.NewGuid();

        _ledger.LogPlayer(player1, "Rating", 1000, 1016, "Battle", "BattleWon", battleId);
        _ledger.LogPlayer(player2, "Rating", 1000, 984, "Battle", "BattleLost", battleId);
        _ledger.LogPlayer(player1, "Currency", 1000, 1050, "Battle", "BattleWon", battleId);
        _ledger.LogPlayer(player1, "Rating", 1016, 1030, "Battle", "BattleWon", otherBattleId);
        await _context.SaveChangesAsync();

        var entries = await _ledger.GetRelatedEntriesAsync(battleId);
        Assert.Equal(3, entries.Count);
        Assert.All(entries, e => Assert.Equal(battleId, e.RelatedEntityId));
    }
}
