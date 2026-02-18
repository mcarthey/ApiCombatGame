using System.Text.Json;
using ApiCombatGame.Data;
using ApiCombatGame.Models.Domain;
using ApiCombatGame.Models.DTOs.BattlePass;
using ApiCombatGame.Models.Enums;
using ApiCombatGame.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ApiCombatGame.Services;

public class BattlePassService : IBattlePassService
{
    private readonly GameDbContext _context;
    private readonly INotificationService _notifications;
    private readonly IActivityLedger _ledger;
    private readonly ILogger<BattlePassService> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public BattlePassService(GameDbContext context, INotificationService notifications, IActivityLedger ledger, ILogger<BattlePassService> logger)
    {
        _context = context;
        _notifications = notifications;
        _ledger = ledger;
        _logger = logger;
    }

    public async Task<BattlePassProgressResponse> GetProgressAsync(Guid playerId)
    {
        var pass = await GetOrCreateActivePassAsync();
        if (pass == null)
            return new BattlePassProgressResponse { PassName = "No active battle pass" };

        var progress = await GetOrCreateProgressAsync(playerId, pass);
        var rewards = JsonSerializer.Deserialize<List<BattlePassRewardDef>>(pass.RewardsJson, JsonOptions) ?? new();
        var claimed = JsonSerializer.Deserialize<List<int>>(progress.ClaimedLevelsJson, JsonOptions) ?? new();

        var rewardList = rewards.Select(r => new BattlePassLevelReward
        {
            Level = r.Level,
            Track = r.Track,
            RewardType = r.RewardType,
            RewardValue = r.RewardValue,
            Description = r.Description,
            Claimed = claimed.Contains(r.Level) && r.Track == "free"
                   || claimed.Contains(r.Level + 100) && r.Track == "premium",
            CanClaim = progress.CurrentLevel >= r.Level
                    && (r.Track == "free" || progress.HasPremium)
                    && !(claimed.Contains(r.Level) && r.Track == "free")
                    && !(claimed.Contains(r.Level + 100) && r.Track == "premium")
        }).ToList();

        int totalXpForMax = pass.MaxLevel * pass.XpPerLevel;
        int totalXpEarned = (progress.CurrentLevel * pass.XpPerLevel) + progress.CurrentXp;

        return new BattlePassProgressResponse
        {
            PassName = pass.Name,
            CurrentLevel = progress.CurrentLevel,
            CurrentXp = progress.CurrentXp,
            XpToNextLevel = progress.CurrentLevel >= pass.MaxLevel ? 0 : pass.XpPerLevel - progress.CurrentXp,
            MaxLevel = pass.MaxLevel,
            OverallProgressPercent = totalXpForMax > 0 ? Math.Round((double)totalXpEarned / totalXpForMax * 100, 1) : 0,
            HasPremium = progress.HasPremium,
            Rewards = rewardList,
            EndsAt = pass.EndsAt,
            DaysRemaining = Math.Max(0, (int)(pass.EndsAt - DateTime.UtcNow).TotalDays)
        };
    }

    public async Task<ClaimRewardResponse> ClaimRewardAsync(Guid playerId, int level)
    {
        var pass = await GetOrCreateActivePassAsync();
        if (pass == null)
            throw new InvalidOperationException("No active battle pass.");

        var progress = await GetOrCreateProgressAsync(playerId, pass);

        if (progress.CurrentLevel < level)
            throw new InvalidOperationException($"You haven't reached level {level} yet. Current level: {progress.CurrentLevel}.");

        var rewards = JsonSerializer.Deserialize<List<BattlePassRewardDef>>(pass.RewardsJson, JsonOptions) ?? new();
        var claimed = JsonSerializer.Deserialize<List<int>>(progress.ClaimedLevelsJson, JsonOptions) ?? new();

        // Find unclaimed rewards at this level
        var freeReward = rewards.FirstOrDefault(r => r.Level == level && r.Track == "free");
        var premiumReward = rewards.FirstOrDefault(r => r.Level == level && r.Track == "premium");

        BattlePassRewardDef? rewardToClaim = null;
        int claimKey = 0;

        // Prioritize unclaimed free reward, then premium
        if (freeReward != null && !claimed.Contains(level))
        {
            rewardToClaim = freeReward;
            claimKey = level;
        }
        else if (premiumReward != null && progress.HasPremium && !claimed.Contains(level + 100))
        {
            rewardToClaim = premiumReward;
            claimKey = level + 100;
        }

        if (rewardToClaim == null)
            throw new InvalidOperationException($"No unclaimed rewards at level {level}.");

        // Grant the reward
        var player = await _context.Players.FindAsync(playerId);
        if (player == null)
            throw new InvalidOperationException("Player not found.");

        var oldCurrency = player.Currency;
        var oldXp = player.ExperiencePoints;

        switch (rewardToClaim.RewardType)
        {
            case "currency":
                player.Currency += rewardToClaim.RewardValue;
                break;
            case "xp":
                player.ExperiencePoints += rewardToClaim.RewardValue;
                break;
            case "title":
                var existingTitle = await _context.Set<PlayerTitle>()
                    .FirstOrDefaultAsync(t => t.Name == rewardToClaim.Description);
                if (existingTitle == null)
                {
                    existingTitle = new PlayerTitle
                    {
                        Id = Guid.NewGuid(),
                        Name = rewardToClaim.Description,
                        Description = $"Earned from Battle Pass level {level}",
                        ColorHex = level >= 25 ? "#FFD700" : "#C0C0C0"
                    };
                    _context.Set<PlayerTitle>().Add(existingTitle);
                }
                player.ActiveTitleId = existingTitle.Id;
                break;
        }

        _ledger.LogPlayer(playerId, "Currency", oldCurrency, player.Currency, "BattlePass", "RewardClaimed");
        _ledger.LogPlayer(playerId, "ExperiencePoints", oldXp, player.ExperiencePoints, "BattlePass", "RewardClaimed");

        claimed.Add(claimKey);
        progress.ClaimedLevelsJson = JsonSerializer.Serialize(claimed, JsonOptions);
        await _context.SaveChangesAsync();

        return new ClaimRewardResponse
        {
            Success = true,
            Level = level,
            Track = rewardToClaim.Track,
            RewardType = rewardToClaim.RewardType,
            RewardValue = rewardToClaim.RewardValue,
            Description = rewardToClaim.Description,
            Message = $"Claimed {rewardToClaim.Description}!"
        };
    }

    public async Task<BattlePassXpGainResponse> AddXpAsync(Guid playerId, int xp, string source)
    {
        var pass = await GetOrCreateActivePassAsync();
        if (pass == null)
            return new BattlePassXpGainResponse();

        var progress = await GetOrCreateProgressAsync(playerId, pass);

        if (progress.CurrentLevel >= pass.MaxLevel)
            return new BattlePassXpGainResponse { NewLevel = progress.CurrentLevel, NewXp = progress.CurrentXp };

        // Check if player is Premium Plus (bonus XP)
        var player = await _context.Players.FindAsync(playerId);
        if (player?.CurrentTier == SubscriptionTier.PremiumPlus)
            xp = (int)(xp * 1.25); // 25% bonus XP for Premium Plus

        int startLevel = progress.CurrentLevel;
        progress.CurrentXp += xp;

        // Level up as many times as possible
        int levelsGained = 0;
        while (progress.CurrentXp >= pass.XpPerLevel && progress.CurrentLevel < pass.MaxLevel)
        {
            progress.CurrentXp -= pass.XpPerLevel;
            progress.CurrentLevel++;
            levelsGained++;
        }

        // Cap XP at max level
        if (progress.CurrentLevel >= pass.MaxLevel)
            progress.CurrentXp = 0;

        await _context.SaveChangesAsync();

        if (levelsGained > 0)
        {
            await _notifications.SendAsync(playerId, NotificationType.AchievementUnlocked,
                "Battle Pass Level Up!",
                $"You reached Battle Pass level {progress.CurrentLevel}! Check for new rewards.",
                "/api/v1/battlepass/progress");

            _logger.LogInformation("Player {PlayerId} reached Battle Pass level {Level} (+{Gained} from {Source})",
                playerId, progress.CurrentLevel, levelsGained, source);
        }

        return new BattlePassXpGainResponse
        {
            XpGained = xp,
            NewXp = progress.CurrentXp,
            NewLevel = progress.CurrentLevel,
            LeveledUp = levelsGained > 0,
            LevelsGained = levelsGained
        };
    }

    private async Task<BattlePass?> GetOrCreateActivePassAsync()
    {
        var pass = await _context.Set<BattlePass>()
            .FirstOrDefaultAsync(bp => bp.IsActive && bp.EndsAt > DateTime.UtcNow);

        if (pass != null) return pass;

        // Create a new battle pass tied to the active season
        var season = await _context.Seasons.FirstOrDefaultAsync(s => s.IsActive);
        if (season == null) return null;

        pass = new BattlePass
        {
            Id = Guid.NewGuid(),
            SeasonId = season.Id,
            Name = $"Season {season.SeasonNumber} Battle Pass",
            MaxLevel = 30,
            XpPerLevel = 1000,
            StartsAt = season.StartDate,
            EndsAt = season.EndDate,
            IsActive = true,
            RewardsJson = JsonSerializer.Serialize(GenerateDefaultRewards(), JsonOptions)
        };

        _context.Set<BattlePass>().Add(pass);
        await _context.SaveChangesAsync();
        return pass;
    }

    private async Task<PlayerBattlePass> GetOrCreateProgressAsync(Guid playerId, BattlePass pass)
    {
        var progress = await _context.Set<PlayerBattlePass>()
            .FirstOrDefaultAsync(p => p.PlayerId == playerId && p.BattlePassId == pass.Id);

        if (progress != null) return progress;

        var player = await _context.Players.FindAsync(playerId);
        bool hasPremium = player?.CurrentTier >= SubscriptionTier.PremiumPlus;

        progress = new PlayerBattlePass
        {
            Id = Guid.NewGuid(),
            PlayerId = playerId,
            BattlePassId = pass.Id,
            HasPremium = hasPremium,
            JoinedAt = DateTime.UtcNow
        };

        _context.Set<PlayerBattlePass>().Add(progress);
        await _context.SaveChangesAsync();
        return progress;
    }

    private static List<BattlePassRewardDef> GenerateDefaultRewards()
    {
        var rewards = new List<BattlePassRewardDef>();

        for (int level = 1; level <= 30; level++)
        {
            // Free track — every level gets a free reward
            if (level % 5 == 0)
            {
                // Milestone levels: bigger rewards
                rewards.Add(new BattlePassRewardDef
                {
                    Level = level,
                    Track = "free",
                    RewardType = "currency",
                    RewardValue = 200 + (level * 10),
                    Description = $"Level {level} Milestone: {200 + (level * 10)}g"
                });
            }
            else if (level % 3 == 0)
            {
                rewards.Add(new BattlePassRewardDef
                {
                    Level = level,
                    Track = "free",
                    RewardType = "xp",
                    RewardValue = 100 + (level * 5),
                    Description = $"{100 + (level * 5)} Bonus XP"
                });
            }
            else
            {
                rewards.Add(new BattlePassRewardDef
                {
                    Level = level,
                    Track = "free",
                    RewardType = "currency",
                    RewardValue = 50 + (level * 5),
                    Description = $"{50 + (level * 5)}g"
                });
            }

            // Premium track — every level also has a premium reward
            if (level == 30)
            {
                rewards.Add(new BattlePassRewardDef
                {
                    Level = level,
                    Track = "premium",
                    RewardType = "title",
                    RewardValue = 1,
                    Description = "Season Champion"
                });
            }
            else if (level % 10 == 0)
            {
                rewards.Add(new BattlePassRewardDef
                {
                    Level = level,
                    Track = "premium",
                    RewardType = "title",
                    RewardValue = 1,
                    Description = level == 10 ? "Pass Veteran" : "Pass Elite"
                });
            }
            else if (level % 5 == 0)
            {
                rewards.Add(new BattlePassRewardDef
                {
                    Level = level,
                    Track = "premium",
                    RewardType = "currency",
                    RewardValue = 500 + (level * 20),
                    Description = $"Premium Milestone: {500 + (level * 20)}g"
                });
            }
            else
            {
                rewards.Add(new BattlePassRewardDef
                {
                    Level = level,
                    Track = "premium",
                    RewardType = "currency",
                    RewardValue = 100 + (level * 10),
                    Description = $"Premium: {100 + (level * 10)}g"
                });
            }
        }

        return rewards;
    }

    private class BattlePassRewardDef
    {
        public int Level { get; set; }
        public string Track { get; set; } = "free";
        public string RewardType { get; set; } = string.Empty;
        public int RewardValue { get; set; }
        public string Description { get; set; } = string.Empty;
    }
}
