using ApiCombatGame.Data;
using ApiCombatGame.Models.Domain;
using ApiCombatGame.Models.Enums;
using ApiCombatGame.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ApiCombatGame.Services;

public class GuildStrategyService : IGuildStrategyService
{
    private readonly GameDbContext _context;
    private readonly IGuildService _guildService;

    public GuildStrategyService(GameDbContext context, IGuildService guildService)
    {
        _context = context;
        _guildService = guildService;
    }

    public async Task<List<GuildStrategy>> GetStrategiesAsync(Guid guildId)
    {
        return await _context.GuildStrategies
            .Include(s => s.Creator)
            .Where(s => s.GuildId == guildId)
            .OrderByDescending(s => s.UsageCount)
            .ThenByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    public async Task<GuildStrategy> PublishAsync(Guid guildId, Guid creatorId, string name, string description, string strategyJson)
    {
        var membership = await _context.GuildMemberships
            .FirstOrDefaultAsync(m => m.GuildId == guildId && m.PlayerId == creatorId)
            ?? throw new InvalidOperationException("You are not a member of this guild.");

        if (!_guildService.HasPermission(membership.Role, GuildPermission.PublishStrategy))
            throw new InvalidOperationException("You do not have permission to publish strategies. Requires Officer or Leader role.");

        var strategy = new GuildStrategy
        {
            Id = Guid.NewGuid(),
            GuildId = guildId,
            CreatorId = creatorId,
            Name = name,
            Description = description,
            StrategyJson = strategyJson,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.GuildStrategies.Add(strategy);
        await _context.SaveChangesAsync();

        await _context.Entry(strategy).Reference(s => s.Creator).LoadAsync();
        return strategy;
    }

    public async Task<GuildStrategy> UpdateAsync(Guid guildId, Guid strategyId, Guid requestingPlayerId, string? name, string? description, string? strategyJson)
    {
        var strategy = await _context.GuildStrategies
            .Include(s => s.Creator)
            .FirstOrDefaultAsync(s => s.Id == strategyId && s.GuildId == guildId)
            ?? throw new KeyNotFoundException("Strategy not found.");

        var guild = await _context.Guilds.FindAsync(guildId)
            ?? throw new KeyNotFoundException("Guild not found.");

        // Only the creator or the guild leader can update
        if (strategy.CreatorId != requestingPlayerId && guild.LeaderId != requestingPlayerId)
            throw new InvalidOperationException("Only the strategy creator or guild leader can update this strategy.");

        if (name != null) strategy.Name = name;
        if (description != null) strategy.Description = description;
        if (strategyJson != null) strategy.StrategyJson = strategyJson;
        strategy.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return strategy;
    }

    public async Task DeleteAsync(Guid guildId, Guid strategyId, Guid requestingPlayerId)
    {
        var strategy = await _context.GuildStrategies
            .FirstOrDefaultAsync(s => s.Id == strategyId && s.GuildId == guildId)
            ?? throw new KeyNotFoundException("Strategy not found.");

        var guild = await _context.Guilds.FindAsync(guildId)
            ?? throw new KeyNotFoundException("Guild not found.");

        // Only the guild leader can delete any strategy
        if (guild.LeaderId != requestingPlayerId && strategy.CreatorId != requestingPlayerId)
            throw new InvalidOperationException("Only the strategy creator or guild leader can delete this strategy.");

        _context.GuildStrategies.Remove(strategy);
        await _context.SaveChangesAsync();
    }
}
