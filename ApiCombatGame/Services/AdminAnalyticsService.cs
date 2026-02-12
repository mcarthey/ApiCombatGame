using ApiCombatGame.Data;
using ApiCombatGame.Models.Domain;
using ApiCombatGame.Models.Enums;
using ApiCombatGame.Models.ViewModels;
using ApiCombatGame.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ApiCombatGame.Services;

public class AdminAnalyticsService : IAdminAnalyticsService
{
    private readonly GameDbContext _context;
    private readonly INotificationService _notifications;
    private readonly ILogger<AdminAnalyticsService> _logger;

    public AdminAnalyticsService(GameDbContext context, INotificationService notifications, ILogger<AdminAnalyticsService> logger)
    {
        _context = context;
        _notifications = notifications;
        _logger = logger;
    }

    private async Task AuditLogAsync(Guid adminPlayerId, string action, Guid? targetPlayerId = null, string? detailsJson = null)
    {
        _context.AdminAuditLogs.Add(new AdminAuditLog
        {
            Id = Guid.NewGuid(),
            AdminPlayerId = adminPlayerId,
            Action = action,
            TargetPlayerId = targetPlayerId,
            DetailsJson = detailsJson,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
    }

    public async Task<AdminOverviewData> GetOverviewAsync()
    {
        var now = DateTime.UtcNow;
        var today = now.Date;
        var weekAgo = today.AddDays(-7);
        var monthAgo = today.AddDays(-30);
        var prevMonthStart = today.AddDays(-60);
        var prevMonthEnd = today.AddDays(-30);

        var totalPlayers = await _context.Players.CountAsync();
        var dau = await _context.Players.CountAsync(p => p.LastLoginAt >= today);
        var wau = await _context.Players.CountAsync(p => p.LastLoginAt >= weekAgo);
        var mau = await _context.Players.CountAsync(p => p.LastLoginAt >= monthAgo);

        var prevDau = await _context.Players.CountAsync(p => p.LastLoginAt >= today.AddDays(-1) && p.LastLoginAt < today);
        var prevWau = await _context.Players.CountAsync(p => p.LastLoginAt >= weekAgo.AddDays(-7) && p.LastLoginAt < weekAgo);
        var prevMau = await _context.Players.CountAsync(p => p.LastLoginAt >= prevMonthStart && p.LastLoginAt < prevMonthEnd);

        // Revenue: count paying users
        var premiumCount = await _context.Players.CountAsync(p => p.CurrentTier == SubscriptionTier.Premium);
        var premiumPlusCount = await _context.Players.CountAsync(p => p.CurrentTier == SubscriptionTier.PremiumPlus);
        var mrr = premiumCount * 5m + premiumPlusCount * 12m;

        // Battles today
        var battlesToday = await _context.Battles.CountAsync(b => b.QueuedAt >= today);
        var battlesThisWeek = await _context.Battles.CountAsync(b => b.QueuedAt >= weekAgo);

        // New signups today
        var signupsToday = await _context.Players.CountAsync(p => p.CreatedAt >= today);
        var signupsThisWeek = await _context.Players.CountAsync(p => p.CreatedAt >= weekAgo);

        // Tier breakdown
        var freeCount = await _context.Players.CountAsync(p => p.CurrentTier == SubscriptionTier.Free);
        var conversionRate = totalPlayers > 0 ? Math.Round((double)(premiumCount + premiumPlusCount) / totalPlayers * 100, 1) : 0;

        // Guild stats
        var totalGuilds = await _context.Guilds.CountAsync();

        return new AdminOverviewData
        {
            TotalPlayers = totalPlayers,
            Dau = dau,
            Wau = wau,
            Mau = mau,
            DauChange = prevDau > 0 ? Math.Round((double)(dau - prevDau) / prevDau * 100, 1) : 0,
            WauChange = prevWau > 0 ? Math.Round((double)(wau - prevWau) / prevWau * 100, 1) : 0,
            MauChange = prevMau > 0 ? Math.Round((double)(mau - prevMau) / prevMau * 100, 1) : 0,
            Mrr = mrr,
            Arr = mrr * 12,
            BattlesToday = battlesToday,
            BattlesThisWeek = battlesThisWeek,
            SignupsToday = signupsToday,
            SignupsThisWeek = signupsThisWeek,
            FreeTierCount = freeCount,
            PremiumCount = premiumCount,
            PremiumPlusCount = premiumPlusCount,
            ConversionRate = conversionRate,
            TotalGuilds = totalGuilds,
            DauMauRatio = mau > 0 ? Math.Round((double)dau / mau, 2) : 0
        };
    }

    public async Task<AdminPlayerAnalyticsData> GetPlayerAnalyticsAsync(string? search, string? tierFilter, int page, int pageSize)
    {
        var query = _context.Players.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(p => p.Username.Contains(search) || p.Email.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(tierFilter) && Enum.TryParse<SubscriptionTier>(tierFilter, out var tier))
        {
            query = query.Where(p => p.CurrentTier == tier);
        }

        var totalCount = await query.CountAsync();

        var players = await query
            .OrderByDescending(p => p.Rating)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new AdminPlayerSummary
            {
                Id = p.Id,
                Username = p.Username,
                Email = p.Email,
                Level = p.Level,
                Rating = p.Rating,
                Currency = p.Currency,
                CurrentTier = p.CurrentTier.ToString(),
                WinStreak = p.WinStreak,
                IsAdmin = p.IsAdmin,
                CreatedAt = p.CreatedAt,
                LastLoginAt = p.LastLoginAt
            })
            .ToListAsync();

        // Get battle counts for these players
        var playerIds = players.Select(p => p.Id).ToList();
        var battleCounts = await _context.Battles
            .Where(b => b.Status == BattleStatus.Completed && (playerIds.Contains(b.Player1Id) || (b.Player2Id.HasValue && playerIds.Contains(b.Player2Id.Value))))
            .ToListAsync();

        foreach (var player in players)
        {
            var total = battleCounts.Count(b => b.Player1Id == player.Id || b.Player2Id == player.Id);
            var wins = battleCounts.Count(b => b.WinnerId == player.Id);
            player.TotalBattles = total;
            player.WinRate = total > 0 ? Math.Round((double)wins / total * 100, 1) : 0;
        }

        return new AdminPlayerAnalyticsData
        {
            Players = players,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
        };
    }

    public async Task<AdminPlayerDetailData?> GetPlayerDetailAsync(Guid playerId)
    {
        var player = await _context.Players
            .Include(p => p.GuildMembership)
                .ThenInclude(m => m!.Guild)
            .Include(p => p.Roster)
            .Include(p => p.Teams)
            .Include(p => p.Subscription)
            .FirstOrDefaultAsync(p => p.Id == playerId);

        if (player == null) return null;

        var totalBattles = await _context.Battles.CountAsync(b =>
            (b.Player1Id == playerId || b.Player2Id == playerId) && b.Status == BattleStatus.Completed);
        var wins = await _context.Battles.CountAsync(b => b.WinnerId == playerId);

        var recentBattles = await _context.Battles
            .Where(b => (b.Player1Id == playerId || b.Player2Id == playerId) && b.Status == BattleStatus.Completed)
            .OrderByDescending(b => b.CompletedAt)
            .Take(20)
            .Select(b => new AdminBattleInfo
            {
                Id = b.Id,
                OpponentId = b.Player1Id == playerId ? b.Player2Id : b.Player1Id,
                IsWin = b.WinnerId == playerId,
                RatingChange = b.Player1Id == playerId ? (b.Player1RatingChange ?? 0) : (b.Player2RatingChange ?? 0),
                CompletedAt = b.CompletedAt ?? DateTime.UtcNow
            })
            .ToListAsync();

        // Get opponent names
        var opponentIds = recentBattles.Where(b => b.OpponentId.HasValue).Select(b => b.OpponentId!.Value).Distinct().ToList();
        var opponentNames = await _context.Players
            .Where(p => opponentIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Username);

        foreach (var battle in recentBattles)
        {
            if (battle.OpponentId.HasValue && opponentNames.TryGetValue(battle.OpponentId.Value, out var name))
                battle.OpponentName = name;
        }

        var achievementCount = await _context.PlayerAchievements.CountAsync(a => a.PlayerId == playerId && a.IsUnlocked);

        return new AdminPlayerDetailData
        {
            Id = player.Id,
            Username = player.Username,
            Email = player.Email,
            Level = player.Level,
            Rating = player.Rating,
            Currency = player.Currency,
            ExperiencePoints = player.ExperiencePoints,
            WinStreak = player.WinStreak,
            CurrentTier = player.CurrentTier.ToString(),
            IsAdmin = player.IsAdmin,
            AdminRole = player.AdminRole.ToString(),
            CreatedAt = player.CreatedAt,
            LastLoginAt = player.LastLoginAt,
            TotalBattles = totalBattles,
            Wins = wins,
            WinRate = totalBattles > 0 ? Math.Round((double)wins / totalBattles * 100, 1) : 0,
            UnitsOwned = player.Roster.Count,
            TeamsConfigured = player.Teams.Count,
            AchievementsUnlocked = achievementCount,
            GuildName = player.GuildMembership?.Guild?.Name,
            GuildRole = player.GuildMembership?.Role.ToString(),
            SubscriptionStatus = player.Subscription?.Status.ToString(),
            RecentBattles = recentBattles
        };
    }

    public async Task<AdminMetaData> GetMetaDataAsync(int days = 7)
    {
        var cutoff = DateTime.UtcNow.AddDays(-days);

        // Get completed battles in the time window
        var battles = await _context.Battles
            .Where(b => b.Status == BattleStatus.Completed && b.CompletedAt >= cutoff)
            .Select(b => new { b.Id, b.WinnerId, b.Player1Id, b.Player2Id, b.Team1ClassesJson, b.Team2ClassesJson })
            .ToListAsync();

        var totalBattles = battles.Count;

        // Unit class win rates from Team1ClassesJson/Team2ClassesJson
        var classStats = new Dictionary<string, (int used, int wins)>();

        foreach (var battle in battles)
        {
            var team1Classes = System.Text.Json.JsonSerializer.Deserialize<List<string>>(battle.Team1ClassesJson) ?? new();
            var team2Classes = System.Text.Json.JsonSerializer.Deserialize<List<string>>(battle.Team2ClassesJson) ?? new();

            foreach (var cls in team1Classes)
            {
                if (!classStats.ContainsKey(cls)) classStats[cls] = (0, 0);
                var (used, wins) = classStats[cls];
                classStats[cls] = (used + 1, battle.WinnerId == battle.Player1Id ? wins + 1 : wins);
            }

            foreach (var cls in team2Classes)
            {
                if (!classStats.ContainsKey(cls)) classStats[cls] = (0, 0);
                var (used, wins) = classStats[cls];
                classStats[cls] = (used + 1, battle.WinnerId == battle.Player2Id ? wins + 1 : wins);
            }
        }

        var totalUsage = classStats.Values.Sum(s => s.used);

        var unitClassStats = classStats.Select(kvp => new UnitClassStat
        {
            ClassName = kvp.Key,
            TimesUsed = kvp.Value.used,
            Wins = kvp.Value.wins,
            WinRate = kvp.Value.used > 0 ? Math.Round((double)kvp.Value.wins / kvp.Value.used * 100, 1) : 0,
            UsageRate = totalUsage > 0 ? Math.Round((double)kvp.Value.used / totalUsage * 100, 1) : 0,
            Status = kvp.Value.used > 0
                ? (double)kvp.Value.wins / kvp.Value.used > 0.60 ? "OP"
                : (double)kvp.Value.wins / kvp.Value.used < 0.40 ? "UP"
                : (double)kvp.Value.wins / kvp.Value.used > 0.55 || (double)kvp.Value.wins / kvp.Value.used < 0.45 ? "Watch"
                : "Balanced"
                : "NoData"
        }).OrderByDescending(s => s.WinRate).ToList();

        // Strategy marketplace stats
        var topStrategies = await _context.Strategies
            .Where(s => s.IsPublic)
            .OrderByDescending(s => s.DownloadCount)
            .Take(10)
            .Select(s => new TopStrategyInfo
            {
                Name = s.Name,
                CreatorName = s.Creator.Username,
                Downloads = s.DownloadCount,
                Rating = s.AverageRating,
                CreatedAt = s.CreatedAt
            })
            .ToListAsync();

        // Current modifier
        var currentModifier = await _context.EnvironmentalModifiers
            .Where(m => m.IsActive)
            .Select(m => new ModifierInfo { Name = m.Name, Description = m.Description, StartDate = m.StartDate, EndDate = m.EndDate })
            .FirstOrDefaultAsync();

        return new AdminMetaData
        {
            TotalBattlesInPeriod = totalBattles,
            Days = days,
            UnitClassStats = unitClassStats,
            TopStrategies = topStrategies,
            CurrentModifier = currentModifier
        };
    }

    public async Task<AdminGuildAnalyticsData> GetGuildAnalyticsAsync()
    {
        var totalGuilds = await _context.Guilds.CountAsync();

        var guilds = await _context.Guilds
            .Include(g => g.Members)
                .ThenInclude(m => m.Player)
            .OrderByDescending(g => g.Level)
            .ThenByDescending(g => g.Members.Count)
            .Take(25)
            .ToListAsync();

        var guildSummaries = guilds.Select(g => new AdminGuildSummary
        {
            Id = g.Id,
            Name = g.Name,
            Tag = g.Tag,
            Level = g.Level,
            MemberCount = g.Members.Count,
            MaxMembers = g.MaxMembers,
            TreasuryBalance = g.TreasuryBalance,
            AvgMemberRating = g.Members.Any() ? (int)g.Members.Average(m => m.Player.Rating) : 0,
            PremiumRate = g.Members.Any() ? Math.Round((double)g.Members.Count(m => m.Player.CurrentTier != SubscriptionTier.Free) / g.Members.Count * 100, 1) : 0,
            CreatedAt = g.CreatedAt
        }).ToList();

        var totalMembers = await _context.GuildMemberships.CountAsync();
        var avgMembers = totalGuilds > 0 ? Math.Round((double)totalMembers / totalGuilds, 1) : 0;

        // Raid stats
        var totalBosses = await _context.GuildBosses.CountAsync();
        var defeatedBosses = await _context.GuildBosses.CountAsync(b => b.IsDefeated);

        return new AdminGuildAnalyticsData
        {
            TotalGuilds = totalGuilds,
            TotalGuildMembers = totalMembers,
            AvgMembersPerGuild = avgMembers,
            TotalBossesSpawned = totalBosses,
            TotalBossesDefeated = defeatedBosses,
            BossCompletionRate = totalBosses > 0 ? Math.Round((double)defeatedBosses / totalBosses * 100, 1) : 0,
            TopGuilds = guildSummaries
        };
    }

    public async Task<AdminTechnicalData> GetTechnicalDataAsync()
    {
        var now = DateTime.UtcNow;
        var today = now.Date;

        // Battle processing stats
        var queuedBattles = await _context.Battles.CountAsync(b => b.Status == BattleStatus.Queued);
        var matchedBattles = await _context.Battles.CountAsync(b => b.Status == BattleStatus.InProgress);
        var completedToday = await _context.Battles.CountAsync(b => b.Status == BattleStatus.Completed && b.CompletedAt >= today);
        var totalBattles = await _context.Battles.CountAsync();
        var completedBattles = await _context.Battles.CountAsync(b => b.Status == BattleStatus.Completed);

        // Database stats
        var playerCount = await _context.Players.CountAsync();
        var unitCount = await _context.Units.CountAsync(u => !u.IsTemplate);
        var teamCount = await _context.Teams.CountAsync();
        var guildCount = await _context.Guilds.CountAsync();
        var strategyCount = await _context.Strategies.CountAsync();
        var challengeCount = await _context.DailyChallenges.CountAsync();
        var achievementUnlocks = await _context.PlayerAchievements.CountAsync(a => a.IsUnlocked);

        return new AdminTechnicalData
        {
            QueuedBattles = queuedBattles,
            MatchedBattles = matchedBattles,
            CompletedBattlesToday = completedToday,
            TotalBattles = totalBattles,
            CompletedBattles = completedBattles,
            TotalPlayers = playerCount,
            TotalUnits = unitCount,
            TotalTeams = teamCount,
            TotalGuilds = guildCount,
            TotalStrategies = strategyCount,
            TotalChallenges = challengeCount,
            TotalAchievementUnlocks = achievementUnlocks
        };
    }

    public async Task<bool> ToggleAdminAsync(Guid adminPlayerId, Guid playerId, bool isAdmin, AdminRole role)
    {
        var player = await _context.Players.FindAsync(playerId);
        if (player == null) return false;

        var oldState = $"IsAdmin={player.IsAdmin}, Role={player.AdminRole}";
        player.IsAdmin = isAdmin;
        player.AdminRole = isAdmin ? role : AdminRole.None;
        await _context.SaveChangesAsync();

        await AuditLogAsync(adminPlayerId, "ToggleAdmin", playerId, $"{{\"old\":\"{oldState}\",\"new\":\"IsAdmin={isAdmin}, Role={role}\"}}");
        await _notifications.SendAsync(playerId, NotificationType.AdminActionOnAccount, "Admin Status Changed",
            isAdmin ? $"You have been granted admin role: {role}" : "Your admin access has been removed.");

        _logger.LogInformation("Admin status changed for {Username}: IsAdmin={IsAdmin}, Role={Role}", player.Username, isAdmin, role);
        return true;
    }

    public async Task<bool> AdjustCurrencyAsync(Guid adminPlayerId, Guid playerId, int amount)
    {
        var player = await _context.Players.FindAsync(playerId);
        if (player == null) return false;

        var oldBalance = player.Currency;
        player.Currency += amount;
        if (player.Currency < 0) player.Currency = 0;
        await _context.SaveChangesAsync();

        await AuditLogAsync(adminPlayerId, "AdjustCurrency", playerId, $"{{\"amount\":{amount},\"oldBalance\":{oldBalance},\"newBalance\":{player.Currency}}}");
        await _notifications.SendAsync(playerId, NotificationType.AdminActionOnAccount, "Currency Adjusted",
            $"An administrator adjusted your gold by {(amount >= 0 ? "+" : "")}{amount}. New balance: {player.Currency}g.");

        _logger.LogInformation("Currency adjusted for {Username}: {Amount} (new balance: {Balance})", player.Username, amount, player.Currency);
        return true;
    }

    public async Task<bool> SetTierAsync(Guid adminPlayerId, Guid playerId, SubscriptionTier tier)
    {
        var player = await _context.Players.FindAsync(playerId);
        if (player == null) return false;

        var oldTier = player.CurrentTier;
        player.CurrentTier = tier;
        await _context.SaveChangesAsync();

        await AuditLogAsync(adminPlayerId, "SetTier", playerId, $"{{\"oldTier\":\"{oldTier}\",\"newTier\":\"{tier}\"}}");
        await _notifications.SendAsync(playerId, NotificationType.TierChanged, "Subscription Tier Changed",
            $"Your subscription tier has been changed from {oldTier} to {tier} by an administrator.");

        // Log subscription event
        _context.SubscriptionEvents.Add(new SubscriptionEvent
        {
            Id = Guid.NewGuid(),
            PlayerId = playerId,
            EventType = oldTier < tier ? "upgraded" : "downgraded",
            OldTier = oldTier,
            NewTier = tier,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        _logger.LogInformation("Tier changed for {Username}: {Tier}", player.Username, tier);
        return true;
    }

    public async Task<bool> ResetPasswordAsync(Guid adminPlayerId, Guid playerId, string newPassword)
    {
        var player = await _context.Players.FindAsync(playerId);
        if (player == null) return false;

        player.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        await _context.SaveChangesAsync();

        await AuditLogAsync(adminPlayerId, "ResetPassword", playerId);
        await _notifications.SendAsync(playerId, NotificationType.PasswordChanged, "Password Reset",
            "Your password was reset by an administrator. If you did not request this, please contact support.");

        _logger.LogInformation("Password reset for {Username}", player.Username);
        return true;
    }
}
