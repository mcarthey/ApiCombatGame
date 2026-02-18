using System.Text.Json;
using ApiCombatGame.Data;
using ApiCombatGame.Models.Domain;
using ApiCombatGame.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ApiCombatGame.Services;

public class AchievementService : IAchievementService
{
    private readonly GameDbContext _context;
    private readonly IPlayerProgressionService _progressionService;
    private readonly ILogger<AchievementService> _logger;
    private readonly INotificationService _notifications;

    private readonly IActivityLedger _ledger;

    public AchievementService(
        GameDbContext context,
        IPlayerProgressionService progressionService,
        ILogger<AchievementService> logger,
        INotificationService notifications,
        IActivityLedger ledger)
    {
        _context = context;
        _progressionService = progressionService;
        _logger = logger;
        _notifications = notifications;
        _ledger = ledger;
    }

    public async Task<List<PlayerAchievement>> GetPlayerAchievementsAsync(Guid playerId)
    {
        return await _context.PlayerAchievements
            .Include(pa => pa.Achievement)
            .Where(pa => pa.PlayerId == playerId)
            .OrderByDescending(pa => pa.UnlockedAt)
            .ToListAsync();
    }

    public async Task CheckAndAwardAsync(Guid playerId, string eventType, Dictionary<string, object>? context = null)
    {
        var achievements = await _context.Achievements.ToListAsync();
        var playerAchievements = await _context.PlayerAchievements
            .Where(pa => pa.PlayerId == playerId)
            .ToListAsync();

        var player = await _context.Players.FindAsync(playerId);
        if (player == null) return;

        var oldAchievementPoints = player.AchievementPoints;

        foreach (var achievement in achievements)
        {
            var playerAch = playerAchievements.FirstOrDefault(pa => pa.AchievementId == achievement.Id);
            if (playerAch?.IsUnlocked == true)
                continue;

            var requirements = JsonSerializer.Deserialize<JsonElement>(achievement.RequirementsJson);
            if (!requirements.TryGetProperty("eventType", out var eventTypeProp))
                continue;

            if (eventTypeProp.GetString() != eventType)
                continue;

            // Create player achievement record if it doesn't exist
            if (playerAch == null)
            {
                int requiredCount = 1;
                if (requirements.TryGetProperty("count", out var countProp))
                    requiredCount = countProp.GetInt32();

                playerAch = new PlayerAchievement
                {
                    Id = Guid.NewGuid(),
                    PlayerId = playerId,
                    AchievementId = achievement.Id,
                    Progress = 0,
                    RequiredProgress = requiredCount
                };
                _context.PlayerAchievements.Add(playerAch);
            }

            // Increment progress
            playerAch.Progress++;

            // Check if achievement is now complete
            if (playerAch.Progress >= playerAch.RequiredProgress)
            {
                playerAch.IsUnlocked = true;
                playerAch.UnlockedAt = DateTime.UtcNow;

                // Award achievement points
                player.AchievementPoints += achievement.Points;

                // Award gold reward if specified
                if (requirements.TryGetProperty("rewardCurrency", out var rewardProp))
                {
                    int reward = rewardProp.GetInt32();
                    await _progressionService.AwardCurrencyAsync(playerId, reward);
                }

                _logger.LogInformation("Player {PlayerId} unlocked achievement '{AchievementName}' (+{Points} pts)",
                    playerId, achievement.Name, achievement.Points);

                await _notifications.SendAsync(playerId, Models.Enums.NotificationType.AchievementUnlocked, "Achievement Unlocked!",
                    $"You unlocked '{achievement.Name}'! +{achievement.Points} achievement points.");
            }
        }

        _ledger.LogPlayer(playerId, "AchievementPoints", oldAchievementPoints, player.AchievementPoints, "Achievement", "AchievementUnlocked");

        await _context.SaveChangesAsync();
    }
}
