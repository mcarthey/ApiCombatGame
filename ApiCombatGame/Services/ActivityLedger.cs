using ApiCombatGame.Data;
using ApiCombatGame.Models.Domain;
using ApiCombatGame.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ApiCombatGame.Services;

public class ActivityLedger : IActivityLedger
{
    private readonly GameDbContext _context;

    public ActivityLedger(GameDbContext context)
    {
        _context = context;
    }

    public void LogPlayer(Guid playerId, string property, long oldValue, long newValue,
        string source, string action, Guid? relatedEntityId = null, string? context = null)
    {
        if (oldValue == newValue) return;

        _context.LedgerEntries.Add(new LedgerEntry
        {
            Id = Guid.NewGuid(),
            EntityId = playerId,
            EntityType = "Player",
            Property = property,
            OldValue = oldValue,
            NewValue = newValue,
            Delta = newValue - oldValue,
            Source = source,
            Action = action,
            RelatedEntityId = relatedEntityId,
            ContextJson = context,
            CreatedAt = DateTime.UtcNow
        });
    }

    public void LogGuild(Guid guildId, string property, long oldValue, long newValue,
        string source, string action, Guid? relatedEntityId = null, string? context = null)
    {
        if (oldValue == newValue) return;

        _context.LedgerEntries.Add(new LedgerEntry
        {
            Id = Guid.NewGuid(),
            EntityId = guildId,
            EntityType = "Guild",
            Property = property,
            OldValue = oldValue,
            NewValue = newValue,
            Delta = newValue - oldValue,
            Source = source,
            Action = action,
            RelatedEntityId = relatedEntityId,
            ContextJson = context,
            CreatedAt = DateTime.UtcNow
        });
    }

    public async Task<List<LedgerEntry>> GetPlayerLedgerAsync(Guid playerId, string? property = null,
        DateTime? since = null, int limit = 50)
    {
        var query = _context.LedgerEntries
            .Where(e => e.EntityId == playerId && e.EntityType == "Player");

        if (property != null)
            query = query.Where(e => e.Property == property);
        if (since.HasValue)
            query = query.Where(e => e.CreatedAt >= since.Value);

        return await query
            .OrderByDescending(e => e.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<List<LedgerEntry>> GetGuildLedgerAsync(Guid guildId, string? property = null,
        DateTime? since = null, int limit = 50)
    {
        var query = _context.LedgerEntries
            .Where(e => e.EntityId == guildId && e.EntityType == "Guild");

        if (property != null)
            query = query.Where(e => e.Property == property);
        if (since.HasValue)
            query = query.Where(e => e.CreatedAt >= since.Value);

        return await query
            .OrderByDescending(e => e.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<List<LedgerEntry>> GetRelatedEntriesAsync(Guid relatedEntityId)
    {
        return await _context.LedgerEntries
            .Where(e => e.RelatedEntityId == relatedEntityId)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();
    }
}
