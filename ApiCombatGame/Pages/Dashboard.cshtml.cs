using System.Security.Claims;
using ApiCombatGame.Data;
using ApiCombatGame.Models.Enums;
using ApiCombatGame.Models.ViewModels;
using ApiCombatGame.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ApiCombatGame.Pages;

[Authorize]
public class DashboardPageModel : PageModel
{
    private readonly GameDbContext _context;
    private readonly IConfiguration _config;
    private readonly IPlayerProgressionService _progression;
    private readonly ILogger<DashboardPageModel> _logger;

    private static readonly int[] StreakRewards = [25, 25, 50, 50, 75, 75, 200];

    public DashboardPageModel(
        GameDbContext context,
        IConfiguration config,
        IPlayerProgressionService progression,
        ILogger<DashboardPageModel> logger)
    {
        _context = context;
        _config = config;
        _progression = progression;
        _logger = logger;
    }

    public PlayerDashboardViewModel Dashboard { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var playerId = GetPlayerId();
        var player = await _context.Players.FirstOrDefaultAsync(p => p.Id == playerId);
        if (player == null) return RedirectToPage("/Auth/Login");

        // Reset daily battles if new day
        if (player.LastBattleResetDate.Date < DateTime.UtcNow.Date)
        {
            player.DailyBattlesUsed = 0;
            player.LastBattleResetDate = DateTime.UtcNow.Date;
        }

        // Process login streak
        var (streakUpdated, todayClaimed, todayReward) = ProcessLoginStreak(player);

        if (streakUpdated)
            await _context.SaveChangesAsync();

        // XP calculations
        var xpToNext = _progression.GetXpForLevel(player.Level);
        var xpPercent = xpToNext > 0 ? Math.Min(100, (int)((double)player.ExperiencePoints / xpToNext * 100)) : 0;

        // Unread notifications
        var unreadCount = await _context.Set<Models.Domain.Notification>()
            .CountAsync(n => n.PlayerId == playerId && !n.IsRead);

        // Achievements
        var achievements = await _context.Set<Models.Domain.Achievement>().ToListAsync();
        var playerAchievements = await _context.Set<Models.Domain.PlayerAchievement>()
            .Where(pa => pa.PlayerId == playerId)
            .ToListAsync();

        var achievementLookup = playerAchievements.ToDictionary(pa => pa.AchievementId);
        var unlockedCount = playerAchievements.Count(pa => pa.IsUnlocked);

        var recentUnlocks = playerAchievements
            .Where(pa => pa.IsUnlocked && pa.UnlockedAt.HasValue)
            .OrderByDescending(pa => pa.UnlockedAt)
            .Take(3)
            .Select(pa =>
            {
                var def = achievements.FirstOrDefault(a => a.Id == pa.AchievementId);
                return def == null ? null : ToAchievementDisplay(def, pa);
            })
            .Where(a => a != null)
            .Cast<AchievementDisplayInfo>()
            .ToList();

        // Next achievement (highest progress % among non-unlocked)
        AchievementDisplayInfo? nextAchievement = null;
        var inProgress = achievements
            .Where(a => !a.IsSecret || achievementLookup.ContainsKey(a.Id))
            .Select(a =>
            {
                achievementLookup.TryGetValue(a.Id, out var pa);
                if (pa != null && pa.IsUnlocked) return null;
                return ToAchievementDisplay(a, pa);
            })
            .Where(a => a != null && a.ProgressPercent > 0)
            .OrderByDescending(a => a!.ProgressPercent)
            .FirstOrDefault();
        nextAchievement = inProgress;

        // Hero roster
        var ownedUnits = await _context.Units
            .Where(u => u.PlayerId == playerId)
            .OrderBy(u => u.Name)
            .ToListAsync();

        var templateUnits = await _context.Units
            .Where(u => u.IsTemplate)
            .ToDictionaryAsync(u => u.Name, u => u.Id);

        var masteryByTemplateId = await _context.UnitMasteries
            .Where(m => m.PlayerId == playerId)
            .ToDictionaryAsync(m => m.UnitId, m => m);

        var heroes = ownedUnits.Select(u =>
        {
            var masteryLevel = 0;
            if (templateUnits.TryGetValue(u.Name, out var templateId) &&
                masteryByTemplateId.TryGetValue(templateId, out var mastery))
            {
                masteryLevel = mastery.Level;
            }
            return new HeroDisplayInfo
            {
                UnitId = u.Id,
                Name = u.Name,
                ClassName = u.Class.ToString(),
                MasteryLevel = masteryLevel,
                Attack = u.Attack,
                Defense = u.Defense,
                Speed = u.Speed,
                Health = u.Health
            };
        }).OrderByDescending(h => h.MasteryLevel).ThenBy(h => h.Name).ToList();

        if (heroes.Count > 0)
            heroes[0].IsHighestMastery = true;

        var totalTemplates = await _context.Units.CountAsync(u => u.IsTemplate);
        var ownedNames = ownedUnits.Select(u => u.Name).ToHashSet();
        var affordableCount = await _context.Units
            .CountAsync(u => u.IsTemplate && u.UnlockCost <= player.Currency && !ownedNames.Contains(u.Name));

        // Daily challenges
        var challenges = await _context.DailyChallenges
            .Where(c => c.PlayerId == playerId && c.ExpiresAt > DateTime.UtcNow)
            .OrderBy(c => c.ExpiresAt)
            .ToListAsync();

        var challengeDisplays = challenges.Select(c =>
        {
            var remaining = c.ExpiresAt - DateTime.UtcNow;
            var timeStr = remaining.TotalHours >= 1
                ? $"{(int)remaining.TotalHours}h {remaining.Minutes}m"
                : $"{remaining.Minutes}m";
            var pct = c.RequiredProgress > 0
                ? Math.Min(100, (int)((double)c.Progress / c.RequiredProgress * 100))
                : 0;
            return new ChallengeDisplayInfo
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                Progress = c.Progress,
                RequiredProgress = c.RequiredProgress,
                ProgressPercent = pct,
                IsCompleted = c.IsCompleted,
                RewardCurrency = c.RewardCurrency,
                RewardXp = c.RewardExperience,
                TimeRemaining = timeStr
            };
        }).ToList();

        // Guild info
        GuildDisplayInfo? guildInfo = null;
        GuildBossDisplayInfo? bossInfo = null;

        var membership = await _context.GuildMemberships
            .Include(m => m.Guild)
            .FirstOrDefaultAsync(m => m.PlayerId == playerId);

        if (membership != null)
        {
            var memberCount = await _context.GuildMemberships
                .CountAsync(m => m.GuildId == membership.GuildId);

            guildInfo = new GuildDisplayInfo
            {
                GuildId = membership.GuildId,
                Name = membership.Guild.Name,
                Tag = membership.Guild.Tag,
                Role = membership.Role.ToString(),
                ContributionPoints = membership.ContributionPoints,
                MemberCount = memberCount
            };

            var activeBoss = await _context.GuildBosses
                .Where(b => b.GuildId == membership.GuildId && !b.IsDefeated && b.ExpiresAt > DateTime.UtcNow)
                .FirstOrDefaultAsync();

            if (activeBoss != null)
            {
                bossInfo = new GuildBossDisplayInfo
                {
                    Name = activeBoss.Name,
                    CurrentHp = activeBoss.CurrentHp,
                    MaxHp = activeBoss.MaxHp,
                    HpPercent = activeBoss.MaxHp > 0
                        ? Math.Max(0, (int)((double)activeBoss.CurrentHp / activeBoss.MaxHp * 100))
                        : 0,
                    IsDefeated = activeBoss.IsDefeated
                };
            }
        }

        // Battle status
        var dailyLimit = player.CurrentTier == SubscriptionTier.Free
            ? _config.GetValue("GameSettings:FreeTierDailyBattleLimit", 10)
            : -1;

        // Build suggested actions
        var actions = BuildSuggestedActions(player, affordableCount, challengeDisplays, guildInfo, bossInfo, unreadCount);

        // Build streak days display
        var streakDays = new List<LoginStreakDay>();
        for (int i = 0; i < 7; i++)
        {
            streakDays.Add(new LoginStreakDay
            {
                Day = i + 1,
                RewardAmount = StreakRewards[i],
                IsClaimed = i < player.LoginStreak,
                IsToday = i == player.LoginStreak - 1 && todayClaimed
            });
        }

        Dashboard = new PlayerDashboardViewModel
        {
            Username = player.Username,
            Level = player.Level,
            ExperiencePoints = player.ExperiencePoints,
            XpToNextLevel = xpToNext,
            XpProgressPercent = xpPercent,
            Currency = player.Currency,
            Rating = player.Rating,
            Tier = player.CurrentTier.ToString(),
            LoginStreak = player.LoginStreak,
            TodayRewardClaimed = todayClaimed,
            TodayRewardAmount = todayReward,
            StreakDays = streakDays,
            AchievementPoints = player.AchievementPoints,
            AchievementsUnlocked = unlockedCount,
            TotalAchievements = achievements.Count(a => !a.IsSecret),
            RecentAchievements = recentUnlocks,
            NextAchievement = nextAchievement,
            Heroes = heroes,
            TotalUnitsAvailable = totalTemplates,
            AffordableUnlockCount = affordableCount,
            Challenges = challengeDisplays,
            Guild = guildInfo,
            GuildBoss = bossInfo,
            BattlesToday = player.DailyBattlesUsed,
            DailyBattleLimit = dailyLimit,
            WinStreak = player.WinStreak,
            HasBattledToday = player.DailyBattlesUsed > 0,
            UnreadNotifications = unreadCount,
            SuggestedActions = actions
        };

        return Page();
    }

    private (bool Updated, bool TodayClaimed, int RewardAmount) ProcessLoginStreak(Models.Domain.Player player)
    {
        var today = DateTime.UtcNow.Date;

        // Already claimed today
        if (player.LastLoginRewardDate.HasValue && player.LastLoginRewardDate.Value.Date == today)
            return (false, true, StreakRewards[Math.Min(player.LoginStreak - 1, 6)]);

        bool isConsecutive = player.LastLoginRewardDate.HasValue
            && player.LastLoginRewardDate.Value.Date == today.AddDays(-1);

        if (isConsecutive && player.LoginStreak < 7)
        {
            player.LoginStreak++;
        }
        else
        {
            // Missed a day, first visit, or completed day 7 cycle
            player.LoginStreak = 1;
        }

        var rewardIndex = Math.Min(player.LoginStreak - 1, 6);
        var reward = StreakRewards[rewardIndex];

        player.Currency += reward;
        player.LastLoginRewardDate = today;
        player.LastLoginRewardClaimedAt = DateTime.UtcNow;

        _logger.LogInformation(
            "Player {PlayerId} claimed login streak day {Day} reward: {Gold}g (streak: {Streak})",
            player.Id, player.LoginStreak, reward, player.LoginStreak);

        return (true, true, reward);
    }

    private static AchievementDisplayInfo ToAchievementDisplay(
        Models.Domain.Achievement def, Models.Domain.PlayerAchievement? pa)
    {
        var progress = pa?.Progress ?? 0;
        var required = pa?.RequiredProgress ?? 1;
        var pct = required > 0 ? Math.Min(100, (int)((double)progress / required * 100)) : 0;

        return new AchievementDisplayInfo
        {
            Name = def.Name,
            Description = def.Description,
            IconUrl = def.IconUrl,
            Category = def.Category,
            Points = def.Points,
            Progress = progress,
            RequiredProgress = required,
            ProgressPercent = pct,
            IsUnlocked = pa?.IsUnlocked ?? false,
            UnlockedAt = pa?.UnlockedAt
        };
    }

    private static List<SuggestedAction> BuildSuggestedActions(
        Models.Domain.Player player,
        int affordableUnlocks,
        List<ChallengeDisplayInfo> challenges,
        GuildDisplayInfo? guild,
        GuildBossDisplayInfo? boss,
        int unreadNotifications)
    {
        var actions = new List<SuggestedAction>();

        if (player.DailyBattlesUsed == 0)
        {
            actions.Add(new SuggestedAction
            {
                Title = "Fight Your First Battle",
                Description = "Earn a +100g first battle bonus today!",
                LinkUrl = "/swagger/index.html#/Battle/post_api_v1_battle_queue",
                LinkText = "API: POST /api/v1/battle/queue",
                IconType = "sword"
            });
        }

        if (challenges.Any(c => c.IsCompleted))
        {
            actions.Add(new SuggestedAction
            {
                Title = "Claim Challenge Rewards",
                Description = $"{challenges.Count(c => c.IsCompleted)} completed challenge(s) with rewards waiting!",
                LinkUrl = "/swagger/index.html#/Challenges/post_api_v1_challenges_claim",
                LinkText = "API: POST /api/v1/challenges/claim",
                IconType = "trophy"
            });
        }

        if (affordableUnlocks > 0)
        {
            actions.Add(new SuggestedAction
            {
                Title = "Unlock a New Hero",
                Description = $"{affordableUnlocks} hero(es) you can afford right now!",
                LinkUrl = "/swagger/index.html#/Player/post_api_v1_player_roster_unlock",
                LinkText = "API: POST /api/v1/player/roster/unlock",
                IconType = "star"
            });
        }

        if (boss != null && !boss.IsDefeated)
        {
            actions.Add(new SuggestedAction
            {
                Title = "Attack the Guild Boss",
                Description = $"{boss.Name} has {boss.HpPercent}% HP remaining!",
                LinkUrl = "/swagger/index.html#/GuildBoss/post_api_v1_guild_boss_attempt",
                LinkText = "API: POST /api/v1/guild/boss/attempt",
                IconType = "guild"
            });
        }

        if (guild == null && player.CurrentTier >= SubscriptionTier.Premium)
        {
            actions.Add(new SuggestedAction
            {
                Title = "Create or Join a Guild",
                Description = "Unlock boss battles, shared treasury, and team strategies!",
                LinkUrl = "/swagger/index.html#/Guild/post_api_v1_guild_create",
                LinkText = "API: POST /api/v1/guild/create",
                IconType = "guild"
            });
        }

        if (player.CurrentTier == SubscriptionTier.Free)
        {
            actions.Add(new SuggestedAction
            {
                Title = "Go Premium",
                Description = "Unlimited battles, guild creation, and 1.5x gold rewards!",
                LinkUrl = "/Account/Subscription",
                LinkText = "View Plans",
                IconType = "coin"
            });
        }

        if (unreadNotifications > 0)
        {
            actions.Add(new SuggestedAction
            {
                Title = "Check Notifications",
                Description = $"{unreadNotifications} unread notification(s)",
                LinkUrl = "/swagger/index.html#/Player/get_api_v1_player_notifications",
                LinkText = "API: GET /api/v1/player/notifications",
                IconType = "shield"
            });
        }

        return actions.Take(6).ToList();
    }

    private Guid GetPlayerId()
    {
        var claim = User.FindFirst("PlayerId") ?? User.FindFirst(ClaimTypes.NameIdentifier);
        return Guid.Parse(claim!.Value);
    }
}
