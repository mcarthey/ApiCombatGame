using ApiCombatGame.Data;
using ApiCombatGame.Models.Enums;
using ApiCombatGame.Models.ViewModels;
using ApiCombatGame.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ApiCombatGame.Services;

public class AdminAnalyticsService : IAdminAnalyticsService
{
    private readonly GameDbContext _context;
    private readonly ILogger<AdminAnalyticsService> _logger;

    public AdminAnalyticsService(GameDbContext context, ILogger<AdminAnalyticsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<AdminOverviewData> GetOverviewAsync()
    {
        var now = DateTime.UtcNow;
        var today = now.Date;
        var weekAgo = today.AddDays(-7);
        var monthAgo = today.AddDays(-30);
        var prevMonthStart = today.AddDays(-60);
        var prevMonthEnd = today.AddDays(-30);

        // All player metrics exclude bots — bots aren't real users
        var realPlayers = _context.Players.Where(p => !p.IsBot);

        var totalPlayers = await realPlayers.CountAsync();
        var dau = await realPlayers.CountAsync(p => p.LastLoginAt >= today);
        var wau = await realPlayers.CountAsync(p => p.LastLoginAt >= weekAgo);
        var mau = await realPlayers.CountAsync(p => p.LastLoginAt >= monthAgo);

        var prevDau = await realPlayers.CountAsync(p => p.LastLoginAt >= today.AddDays(-1) && p.LastLoginAt < today);
        var prevWau = await realPlayers.CountAsync(p => p.LastLoginAt >= weekAgo.AddDays(-7) && p.LastLoginAt < weekAgo);
        var prevMau = await realPlayers.CountAsync(p => p.LastLoginAt >= prevMonthStart && p.LastLoginAt < prevMonthEnd);

        // Revenue: sum actual Stripe subscription amounts — exclude admin/bot accounts
        var mrr = await _context.Subscriptions
            .Where(s => (s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.PastDue)
                && !s.Player.IsAdmin && !s.Player.IsBot)
            .SumAsync(s => s.AmountUsd);

        // Battles today
        var battlesToday = await _context.Battles.CountAsync(b => b.QueuedAt >= today);
        var battlesThisWeek = await _context.Battles.CountAsync(b => b.QueuedAt >= weekAgo);

        // New signups today (real players only)
        var signupsToday = await realPlayers.CountAsync(p => p.CreatedAt >= today);
        var signupsThisWeek = await realPlayers.CountAsync(p => p.CreatedAt >= weekAgo);

        // Tier breakdown (real non-admin players only — admin subs skew business metrics)
        var payingPlayers = realPlayers.Where(p => !p.IsAdmin);
        var payingPlayerCount = await payingPlayers.CountAsync();
        var freeCount = await payingPlayers.CountAsync(p => p.CurrentTier == SubscriptionTier.Free);
        var premiumCount = await payingPlayers.CountAsync(p => p.CurrentTier == SubscriptionTier.Premium);
        var premiumPlusCount = await payingPlayers.CountAsync(p => p.CurrentTier == SubscriptionTier.PremiumPlus);
        var conversionRate = payingPlayerCount > 0 ? Math.Round((double)(premiumCount + premiumPlusCount) / payingPlayerCount * 100, 1) : 0;

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

    public async Task<AdminPlayerAnalyticsData> GetPlayerAnalyticsAsync(string? search, string? tierFilter, int page, int pageSize, bool hideBots = false, string? sortBy = null, bool sortDesc = true)
    {
        var query = _context.Players.AsQueryable();

        if (hideBots)
        {
            query = query.Where(p => !p.IsBot);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(p => p.Username.Contains(search) || p.Email.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(tierFilter) && Enum.TryParse<SubscriptionTier>(tierFilter, out var tier))
        {
            query = query.Where(p => p.CurrentTier == tier);
        }

        var totalCount = await query.CountAsync();

        query = sortBy?.ToLowerInvariant() switch
        {
            "username" => sortDesc ? query.OrderByDescending(p => p.Username) : query.OrderBy(p => p.Username),
            "rating" => sortDesc ? query.OrderByDescending(p => p.Rating) : query.OrderBy(p => p.Rating),
            "tier" => sortDesc ? query.OrderByDescending(p => p.CurrentTier) : query.OrderBy(p => p.CurrentTier),
            "joined" => sortDesc ? query.OrderByDescending(p => p.CreatedAt) : query.OrderBy(p => p.CreatedAt),
            "lastactive" => sortDesc ? query.OrderByDescending(p => p.LastLoginAt) : query.OrderBy(p => p.LastLoginAt),
            "level" => sortDesc ? query.OrderByDescending(p => p.Level) : query.OrderBy(p => p.Level),
            _ => query.OrderByDescending(p => p.Rating)
        };

        var players = await query
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
                IsEducator = p.IsEducator,
                IsBot = p.IsBot,
                CreatedAt = p.CreatedAt,
                LastLoginAt = p.LastLoginAt
            })
            .ToListAsync();

        // Get battle counts for these players (project only IDs to avoid loading full entities)
        var playerIds = players.Select(p => p.Id).ToList();
        var battleData = await _context.Battles
            .Where(b => b.Status == BattleStatus.Completed && (playerIds.Contains(b.Player1Id) || (b.Player2Id.HasValue && playerIds.Contains(b.Player2Id.Value))))
            .Select(b => new { b.Player1Id, b.Player2Id, b.WinnerId })
            .ToListAsync();

        foreach (var player in players)
        {
            var total = battleData.Count(b => b.Player1Id == player.Id || b.Player2Id == player.Id);
            var wins = battleData.Count(b => b.WinnerId == player.Id);
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
                Mode = b.Mode,
                CompletedAt = b.CompletedAt ?? DateTime.UtcNow
            })
            .ToListAsync();

        // Get opponent names
        var opponentIds = recentBattles.Where(b => b.OpponentId.HasValue).Select(b => b.OpponentId!.Value).Distinct().ToList();
        var opponentNames = await _context.Players
            .AsNoTracking()
            .Where(p => opponentIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Username);

        foreach (var battle in recentBattles)
        {
            if (battle.OpponentId.HasValue && opponentNames.TryGetValue(battle.OpponentId.Value, out var name))
                battle.OpponentName = name;
        }

        var achievementCount = await _context.PlayerAchievements.CountAsync(a => a.PlayerId == playerId && a.IsUnlocked);

        // Subscription history (last 20 events)
        var subscriptionHistory = await _context.SubscriptionEvents
            .Where(e => e.PlayerId == playerId)
            .OrderByDescending(e => e.CreatedAt)
            .Take(20)
            .Select(e => new AdminSubscriptionEventEntry
            {
                EventType = e.EventType,
                OldTier = e.OldTier.HasValue ? e.OldTier.Value.ToString() : null,
                NewTier = e.NewTier.HasValue ? e.NewTier.Value.ToString() : null,
                AmountUsd = e.AmountUsd,
                CreatedAt = e.CreatedAt
            })
            .ToListAsync();

        // API keys with usage stats (pre-fetch aggregation to avoid correlated subqueries)
        var playerKeyIds = await _context.ApiKeys
            .Where(k => k.PlayerId == playerId)
            .Select(k => k.Id)
            .ToListAsync();

        var keyUsageStats = await _context.ApiKeyUsageLogs
            .Where(l => playerKeyIds.Contains(l.ApiKeyId))
            .GroupBy(l => l.ApiKeyId)
            .Select(g => new { KeyId = g.Key, TotalRequests = g.Count(), UniqueIps = g.Select(l => l.IpAddress).Distinct().Count() })
            .ToDictionaryAsync(x => x.KeyId);

        var apiKeys = await _context.ApiKeys
            .Where(k => k.PlayerId == playerId)
            .OrderByDescending(k => k.CreatedAt)
            .Select(k => new AdminApiKeyEntry
            {
                Id = k.Id,
                Name = k.Name,
                KeyPrefix = k.KeyPrefix,
                IsActive = k.IsActive,
                CreatedAt = k.CreatedAt,
                LastUsedAt = k.LastUsedAt,
                RevokedAt = k.RevokedAt
            })
            .ToListAsync();

        foreach (var key in apiKeys)
        {
            if (keyUsageStats.TryGetValue(key.Id, out var usage))
            {
                key.TotalRequests = usage.TotalRequests;
                key.UniqueIps = usage.UniqueIps;
            }
        }

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
            IsEducator = player.IsEducator,
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
            RecentBattles = recentBattles,
            SubscriptionHistory = subscriptionHistory,
            ApiKeys = apiKeys
        };
    }

    public async Task<AdminMetaData> GetMetaDataAsync(int days = 7)
    {
        var cutoff = DateTime.UtcNow.AddDays(-days);

        var battles = await _context.Battles
            .Where(b => b.Status == BattleStatus.Completed && b.CompletedAt >= cutoff)
            .Select(b => new { b.Id, b.WinnerId, b.Player1Id, b.Player2Id, b.Team1ClassesJson, b.Team2ClassesJson })
            .ToListAsync();

        var totalBattles = battles.Count;

        var classStats = new Dictionary<string, (int used, int wins)>();

        foreach (var battle in battles)
        {
            var team1Classes = System.Text.Json.JsonSerializer.Deserialize<List<string>>(battle.Team1ClassesJson) ?? new();
            var team2Classes = System.Text.Json.JsonSerializer.Deserialize<List<string>>(battle.Team2ClassesJson) ?? new();

            foreach (var cls in team1Classes)
            {
                if (!classStats.ContainsKey(cls)) classStats[cls] = (0, 0);
                var (used, wins2) = classStats[cls];
                classStats[cls] = (used + 1, battle.WinnerId == battle.Player1Id ? wins2 + 1 : wins2);
            }

            foreach (var cls in team2Classes)
            {
                if (!classStats.ContainsKey(cls)) classStats[cls] = (0, 0);
                var (used, wins2) = classStats[cls];
                classStats[cls] = (used + 1, battle.WinnerId == battle.Player2Id ? wins2 + 1 : wins2);
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
            .AsNoTracking()
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

        var queuedBattles = await _context.Battles.CountAsync(b => b.Status == BattleStatus.Queued);
        var matchedBattles = await _context.Battles.CountAsync(b => b.Status == BattleStatus.InProgress);
        var completedToday = await _context.Battles.CountAsync(b => b.Status == BattleStatus.Completed && b.CompletedAt >= today);
        var totalBattles = await _context.Battles.CountAsync();
        var completedBattles = await _context.Battles.CountAsync(b => b.Status == BattleStatus.Completed);

        var playerCount = await _context.Players.CountAsync();
        var unitCount = await _context.Units.CountAsync(u => !u.IsTemplate);
        var teamCount = await _context.Teams.CountAsync();
        var guildCount = await _context.Guilds.CountAsync();
        var strategyCount = await _context.Strategies.CountAsync();
        var challengeCount = await _context.DailyChallenges.CountAsync();
        var achievementUnlocks = await _context.PlayerAchievements.CountAsync(a => a.IsUnlocked);

        var queueDetails = (await _context.Battles
            .Where(b => b.Status == BattleStatus.Queued)
            .Include(b => b.Player1)
            .OrderBy(b => b.QueuedAt)
            .Take(50)
            .Select(b => new QueuedBattleInfo
            {
                BattleId = b.Id,
                PlayerName = b.Player1.Username,
                PlayerRating = b.Player1.Rating,
                IsBot = b.Player1.IsBot,
                QueuedAt = b.QueuedAt,
                Mode = b.Mode
            })
            .ToListAsync())
            .Select(b => { b.WaitSeconds = (now - b.QueuedAt).TotalSeconds; return b; })
            .ToList();

        var totalNotifications = await _context.Notifications.CountAsync();
        var unreadNotifications = await _context.Notifications.CountAsync(n => !n.IsRead);
        var notificationsSentToday = await _context.Notifications.CountAsync(n => n.CreatedAt >= today);
        var pendingAlerts = await _context.AdminAlerts.CountAsync(a => !a.IsAcknowledged);

        var hourAgo = DateTime.UtcNow.AddHours(-1);
        var errorsToday = await _context.AppLogs.CountAsync(l => l.Level == AppLogLevel.Error && l.CreatedAt >= today);
        var errorsThisHour = await _context.AppLogs.CountAsync(l => l.Level == AppLogLevel.Error && l.CreatedAt >= hourAgo);

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
            TotalAchievementUnlocks = achievementUnlocks,
            QueuedBattleDetails = queueDetails,
            TotalNotifications = totalNotifications,
            UnreadNotifications = unreadNotifications,
            NotificationsSentToday = notificationsSentToday,
            PendingAlerts = pendingAlerts,
            ErrorsToday = errorsToday,
            ErrorsThisHour = errorsThisHour
        };
    }
}
