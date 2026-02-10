using ApiCombatGame.Data;
using ApiCombatGame.Models.Domain;
using ApiCombatGame.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ApiCombatGame.Services;

public class GuildBossService : IGuildBossService
{
    private readonly GameDbContext _context;
    private readonly ILogger<GuildBossService> _logger;

    public GuildBossService(GameDbContext context, ILogger<GuildBossService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<GuildBoss?> GetActiveGuildBoss(Guid guildId)
    {
        var now = DateTime.UtcNow;
        return await _context.GuildBosses
            .Include(b => b.Attempts)
            .FirstOrDefaultAsync(b =>
                b.GuildId == guildId &&
                !b.IsDefeated &&
                b.ExpiresAt > now);
    }

    public async Task<GuildBossAttempt> AttemptBoss(Guid guildBossId, Guid playerId, Guid teamId)
    {
        // TODO: Implement boss battle logic
        // 1. Load boss and player team
        // 2. Load boss abilities from AbilitiesJson
        // 3. Run battle simulation against boss
        // 4. Calculate damage dealt to boss
        // 5. Update boss CurrentHp
        // 6. Check if boss is defeated (CurrentHp <= 0)
        // 7. If defeated: mark IsDefeated, set DefeatedAt, award rewards to guild
        // 8. Create and return GuildBossAttempt record
        throw new NotImplementedException("TODO: Phase 3 implementation - Boss battle logic");
    }

    public async Task<List<GuildBossAttempt>> GetBossLeaderboard(Guid guildBossId)
    {
        return await _context.GuildBossAttempts
            .Where(a => a.GuildBossId == guildBossId)
            .OrderByDescending(a => a.DamageDealt)
            .Take(10)
            .Include(a => a.Player)
            .ToListAsync();
    }

    public async Task SpawnBossForGuild(Guid guildId)
    {
        // Check if guild already has an active boss
        var existingBoss = await GetActiveGuildBoss(guildId);
        if (existingBoss != null)
        {
            _logger.LogInformation("Guild {GuildId} already has an active boss", guildId);
            return;
        }

        // Get guild member count for HP scaling
        var memberCount = await _context.GuildMemberships
            .CountAsync(m => m.GuildId == guildId);

        // TODO: Implement boss type selection and ability assignment
        // For now, create a basic boss with scaled stats
        var boss = new GuildBoss
        {
            Id = Guid.NewGuid(),
            Name = "Ancient Dragon",
            Description = "A fearsome dragon terrorizes the guild's domain!",
            BossType = "BasicDragon",
            AbilitiesJson = "[{\"Type\":\"ScalesHarden\"},{\"Type\":\"Enrage\"}]",
            GuildId = guildId,
            MaxHp = 10000 * Math.Max(1, memberCount),
            CurrentHp = 10000 * Math.Max(1, memberCount),
            Attack = 50 + (memberCount * 5),
            Defense = 30 + (memberCount * 3),
            SpawnedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            RewardCurrency = 1000 * Math.Max(1, memberCount),
            RewardExperience = 500 * Math.Max(1, memberCount)
        };

        _context.GuildBosses.Add(boss);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Spawned boss {BossName} for guild {GuildId} with {Hp} HP",
            boss.Name, guildId, boss.MaxHp);
    }
}
