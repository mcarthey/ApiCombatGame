using System.Text.Json;
using ApiCombatGame.Data;
using ApiCombatGame.Models.Domain;
using ApiCombatGame.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ApiCombatGame.Services;

public class GuildBossService : IGuildBossService
{
    private readonly GameDbContext _context;
    private readonly IPlayerProgressionService _progressionService;
    private readonly IAchievementService _achievementService;
    private readonly INotificationService _notifications;
    private readonly ILogger<GuildBossService> _logger;

    private readonly IActivityLedger _ledger;

    public GuildBossService(
        GameDbContext context,
        IPlayerProgressionService progressionService,
        IAchievementService achievementService,
        INotificationService notifications,
        IActivityLedger ledger,
        ILogger<GuildBossService> logger)
    {
        _context = context;
        _progressionService = progressionService;
        _achievementService = achievementService;
        _notifications = notifications;
        _ledger = ledger;
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
        var boss = await _context.GuildBosses
            .Include(b => b.Attempts)
            .FirstOrDefaultAsync(b => b.Id == guildBossId)
            ?? throw new KeyNotFoundException("Boss not found.");

        if (boss.IsDefeated)
            throw new InvalidOperationException("Boss has already been defeated.");

        if (boss.ExpiresAt <= DateTime.UtcNow)
            throw new InvalidOperationException("Boss encounter has expired.");

        // Verify player is in the guild
        var membership = await _context.GuildMemberships
            .Include(m => m.Guild)
            .FirstOrDefaultAsync(m => m.PlayerId == playerId && m.GuildId == boss.GuildId)
            ?? throw new InvalidOperationException("You are not a member of this guild.");

        // Check daily attempt limit
        var today = DateTime.UtcNow.Date;
        var maxAttempts = membership.Guild.MaxRaidAttempts;
        var todayAttempts = boss.Attempts.Count(a => a.PlayerId == playerId && a.AttemptedAt.Date == today);
        if (todayAttempts >= maxAttempts)
            throw new InvalidOperationException($"Daily raid attempt limit reached ({maxAttempts}/day). Guild upgrade 'Raid Stamina' increases this limit.");

        // Load team and units
        var team = await _context.Teams.FirstOrDefaultAsync(t => t.Id == teamId && t.PlayerId == playerId)
            ?? throw new KeyNotFoundException("Team not found.");

        var unitIds = JsonSerializer.Deserialize<List<Guid>>(team.UnitIdsJson) ?? new();
        var units = await _context.Units.Where(u => unitIds.Contains(u.Id)).ToListAsync();

        if (units.Count == 0)
            throw new InvalidOperationException("Team has no units.");

        // Simplified damage calculation:
        // Each unit attacks for 10 simulated turns
        // Damage per unit per turn = max(1, unit.Attack - boss.Defense / unitCount)
        int totalDamage = 0;
        var logEntries = new List<object>();

        foreach (var unit in units)
        {
            int damagePerTurn = Math.Max(1, unit.Attack - boss.Defense / units.Count);
            int unitDamage = damagePerTurn * 10;
            totalDamage += unitDamage;

            logEntries.Add(new
            {
                unit = unit.Name,
                unitClass = unit.Class.ToString(),
                damagePerTurn,
                turns = 10,
                totalDamage = unitDamage
            });
        }

        // Apply damage to boss
        boss.CurrentHp = Math.Max(0, boss.CurrentHp - totalDamage);
        bool killingBlow = boss.CurrentHp <= 0;

        // Create attempt record
        var attempt = new GuildBossAttempt
        {
            Id = Guid.NewGuid(),
            GuildBossId = guildBossId,
            PlayerId = playerId,
            DamageDealt = totalDamage,
            WasKillingBlow = killingBlow,
            BattleLogJson = JsonSerializer.Serialize(logEntries),
            AttemptedAt = DateTime.UtcNow
        };

        membership.ContributionPoints += totalDamage / 10;
        _context.GuildBossAttempts.Add(attempt);

        if (killingBlow)
        {
            // Use an explicit transaction so boss defeat, treasury deposit, and all
            // contributor rewards succeed or fail atomically — no partial payouts.
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                boss.IsDefeated = true;
                boss.DefeatedAt = DateTime.UtcNow;

                // Deposit reward to guild treasury
                var oldTreasury = membership.Guild.TreasuryBalance;
                membership.Guild.TreasuryBalance += boss.RewardCurrency;
                _ledger.LogGuild(boss.GuildId, "TreasuryBalance", oldTreasury, membership.Guild.TreasuryBalance, "GuildBoss", "BossDefeated", boss.Id);

                // Save boss state + attempt before awarding rewards (progression service does its own saves)
                await _context.SaveChangesAsync();

                // Award XP and gold to all contributors
                var contributors = boss.Attempts
                    .Select(a => a.PlayerId)
                    .Append(playerId)
                    .Distinct()
                    .ToList();

                foreach (var contributorId in contributors)
                {
                    await _progressionService.AwardCurrencyAsync(contributorId, boss.RewardCurrency / contributors.Count);
                    await _progressionService.AwardExperienceAsync(contributorId, boss.RewardExperience / contributors.Count);
                }

                await transaction.CommitAsync();

                _logger.LogInformation("Boss {BossName} defeated by {PlayerId} (killing blow). {ContributorCount} contributors rewarded.",
                    boss.Name, playerId, contributors.Count);

                // Achievement and notifications are outside the transaction — non-critical side effects
                await _achievementService.CheckAndAwardAsync(playerId, "boss_killing_blow");

                await _notifications.SendToGuildAsync(boss.GuildId, Models.Enums.NotificationType.GuildBossDefeated, "Boss Defeated!",
                    $"{(await _context.Players.FindAsync(playerId))?.Username} landed the killing blow on {boss.Name}! Rewards deposited to guild treasury.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Failed to process boss defeat rewards for boss {BossId}. Transaction rolled back — no partial rewards distributed.", boss.Id);
                throw;
            }
        }
        else
        {
            await _context.SaveChangesAsync();
        }

        return attempt;
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
        var existingBoss = await GetActiveGuildBoss(guildId);
        if (existingBoss != null)
        {
            _logger.LogInformation("Guild {GuildId} already has an active boss", guildId);
            return;
        }

        var memberCount = await _context.GuildMemberships
            .CountAsync(m => m.GuildId == guildId);

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

        await _notifications.SendToGuildAsync(guildId, Models.Enums.NotificationType.GuildBossSpawned, "Boss Spawned!",
            $"A new boss has appeared: {boss.Name}! Rally your guild to defeat it before it expires.");

        _logger.LogInformation("Spawned boss {BossName} for guild {GuildId} with {Hp} HP",
            boss.Name, guildId, boss.MaxHp);
    }
}
