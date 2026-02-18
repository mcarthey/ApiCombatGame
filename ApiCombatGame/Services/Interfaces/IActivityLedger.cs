using ApiCombatGame.Models.Domain;

namespace ApiCombatGame.Services.Interfaces;

/// <summary>
/// Records before/after snapshots of every meaningful player and guild state change.
/// Log methods are synchronous — they add entities to the EF change tracker.
/// The calling service's SaveChangesAsync() commits everything atomically.
/// </summary>
public interface IActivityLedger
{
    /// <summary>Log a player property change. Skips if oldValue == newValue.</summary>
    void LogPlayer(Guid playerId, string property, long oldValue, long newValue,
        string source, string action, Guid? relatedEntityId = null, string? context = null);

    /// <summary>Log a guild property change. Skips if oldValue == newValue.</summary>
    void LogGuild(Guid guildId, string property, long oldValue, long newValue,
        string source, string action, Guid? relatedEntityId = null, string? context = null);

    /// <summary>Query ledger entries for a player (most recent first).</summary>
    Task<List<LedgerEntry>> GetPlayerLedgerAsync(Guid playerId, string? property = null,
        DateTime? since = null, int limit = 50);

    /// <summary>Query ledger entries for a guild (most recent first).</summary>
    Task<List<LedgerEntry>> GetGuildLedgerAsync(Guid guildId, string? property = null,
        DateTime? since = null, int limit = 50);

    /// <summary>Get all entries linked to a specific related entity (battle, boss, etc.).</summary>
    Task<List<LedgerEntry>> GetRelatedEntriesAsync(Guid relatedEntityId);
}
