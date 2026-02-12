using ApiCombatGame.Data;
using ApiCombatGame.Models.Domain;
using ApiCombatGame.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ApiCombatGame.Services;

public class MasteryService : IMasteryService
{
    private readonly GameDbContext _context;
    private readonly ILogger<MasteryService> _logger;
    private readonly INotificationService _notifications;

    // Experience required per level (index = level, value = total XP needed)
    private static readonly int[] LevelThresholds = { 0, 0, 100, 300, 600, 1000, 1500, 2100, 2800, 3600, 4500 };

    public MasteryService(GameDbContext context, ILogger<MasteryService> logger, INotificationService notifications)
    {
        _context = context;
        _logger = logger;
        _notifications = notifications;
    }

    public async Task<List<UnitMastery>> GetPlayerMastery(Guid playerId)
    {
        return await _context.UnitMasteries
            .Where(m => m.PlayerId == playerId)
            .OrderByDescending(m => m.Level)
            .ThenByDescending(m => m.ExperiencePoints)
            .ToListAsync();
    }

    public async Task<UnitMastery?> GetUnitMastery(Guid playerId, Guid unitId)
    {
        return await _context.UnitMasteries
            .FirstOrDefaultAsync(m => m.PlayerId == playerId && m.UnitId == unitId);
    }

    public async Task IncrementMastery(Guid playerId, Guid unitId, bool won)
    {
        var mastery = await _context.UnitMasteries
            .FirstOrDefaultAsync(m => m.PlayerId == playerId && m.UnitId == unitId);

        if (mastery == null)
        {
            mastery = new UnitMastery
            {
                Id = Guid.NewGuid(),
                PlayerId = playerId,
                UnitId = unitId,
                Level = 1,
                ExperiencePoints = 0,
                BattlesUsed = 0,
                WinsWithUnit = 0,
                FirstUsed = DateTime.UtcNow,
                LastUsed = DateTime.UtcNow
            };
            _context.UnitMasteries.Add(mastery);
        }

        mastery.BattlesUsed++;
        mastery.LastUsed = DateTime.UtcNow;

        if (won)
        {
            mastery.WinsWithUnit++;
            mastery.ExperiencePoints += 50; // Win bonus
        }
        else
        {
            mastery.ExperiencePoints += 20; // Participation
        }

        // Check for level up (max level 10)
        while (mastery.Level < 10 &&
               mastery.Level < LevelThresholds.Length &&
               mastery.ExperiencePoints >= LevelThresholds[mastery.Level])
        {
            mastery.Level++;
            _logger.LogInformation("Player {PlayerId} reached mastery level {Level} with unit {UnitId}",
                playerId, mastery.Level, unitId);
            var unit = await _context.Units.FindAsync(unitId);
            await _notifications.SendAsync(playerId, Models.Enums.NotificationType.MasteryRankUp, "Mastery Level Up!",
                $"Your mastery of {unit?.Name ?? "unit"} reached level {mastery.Level}!");
        }

        await _context.SaveChangesAsync();
    }
}
