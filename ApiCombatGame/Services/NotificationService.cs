using System.Text.Json;
using ApiCombatGame.Data;
using ApiCombatGame.Models.Domain;
using ApiCombatGame.Models.Enums;
using ApiCombatGame.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ApiCombatGame.Services;

public class NotificationService : INotificationService
{
    private readonly GameDbContext _context;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(GameDbContext context, ILogger<NotificationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SendAsync(Guid playerId, NotificationType type, string title, string message, string? actionUrl = null)
    {
        var category = GetCategory(type);

        // Check preferences (Security and System are always on)
        if (category != NotificationCategory.Security && category != NotificationCategory.System)
        {
            var prefs = await GetPreferencesAsync(playerId);
            if (!ShouldNotify(prefs, category))
                return;
        }

        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            PlayerId = playerId,
            Type = type,
            Category = category,
            Title = title,
            Message = message,
            ActionUrl = actionUrl,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();
    }

    public async Task SendToGuildAsync(Guid guildId, NotificationType type, string title, string message, string? actionUrl = null, Guid? excludePlayerId = null)
    {
        var memberIds = await _context.GuildMemberships
            .Where(m => m.GuildId == guildId)
            .Select(m => m.PlayerId)
            .ToListAsync();

        if (excludePlayerId.HasValue)
            memberIds.Remove(excludePlayerId.Value);

        var category = GetCategory(type);

        // Load all member preferences at once
        var players = await _context.Players
            .Where(p => memberIds.Contains(p.Id))
            .Select(p => new { p.Id, p.NotificationPreferencesJson })
            .ToListAsync();

        var notifications = new List<Notification>();

        foreach (var player in players)
        {
            var prefs = DeserializePreferences(player.NotificationPreferencesJson);
            if (category != NotificationCategory.Security && category != NotificationCategory.System && !ShouldNotify(prefs, category))
                continue;

            notifications.Add(new Notification
            {
                Id = Guid.NewGuid(),
                PlayerId = player.Id,
                Type = type,
                Category = category,
                Title = title,
                Message = message,
                ActionUrl = actionUrl,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(30)
            });
        }

        if (notifications.Count > 0)
        {
            _context.Notifications.AddRange(notifications);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<int> GetUnreadCountAsync(Guid playerId)
    {
        return await _context.Notifications
            .CountAsync(n => n.PlayerId == playerId && !n.IsRead && (n.ExpiresAt == null || n.ExpiresAt > DateTime.UtcNow));
    }

    public async Task<List<Notification>> GetNotificationsAsync(Guid playerId, int page = 1, int pageSize = 20, bool unreadOnly = false)
    {
        var query = _context.Notifications
            .Where(n => n.PlayerId == playerId && (n.ExpiresAt == null || n.ExpiresAt > DateTime.UtcNow));

        if (unreadOnly)
            query = query.Where(n => !n.IsRead);

        return await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task MarkReadAsync(Guid notificationId, Guid playerId)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.PlayerId == playerId);

        if (notification != null)
        {
            notification.IsRead = true;
            await _context.SaveChangesAsync();
        }
    }

    public async Task MarkAllReadAsync(Guid playerId)
    {
        var unread = await _context.Notifications
            .Where(n => n.PlayerId == playerId && !n.IsRead)
            .ToListAsync();

        foreach (var n in unread)
            n.IsRead = true;

        await _context.SaveChangesAsync();
    }

    public async Task DeleteExpiredAsync()
    {
        var expired = await _context.Notifications
            .Where(n => n.ExpiresAt != null && n.ExpiresAt <= DateTime.UtcNow)
            .ToListAsync();

        if (expired.Count > 0)
        {
            _context.Notifications.RemoveRange(expired);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Cleaned up {Count} expired notifications", expired.Count);
        }

        // Also delete read notifications older than 7 days
        var oldRead = await _context.Notifications
            .Where(n => n.IsRead && n.CreatedAt < DateTime.UtcNow.AddDays(-7))
            .ToListAsync();

        if (oldRead.Count > 0)
        {
            _context.Notifications.RemoveRange(oldRead);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Cleaned up {Count} old read notifications", oldRead.Count);
        }
    }

    public async Task<NotificationPreferences> GetPreferencesAsync(Guid playerId)
    {
        var player = await _context.Players
            .AsNoTracking()
            .Where(p => p.Id == playerId)
            .Select(p => p.NotificationPreferencesJson)
            .FirstOrDefaultAsync();

        return DeserializePreferences(player);
    }

    public async Task UpdatePreferencesAsync(Guid playerId, NotificationPreferences preferences)
    {
        var player = await _context.Players.FindAsync(playerId);
        if (player == null) return;

        player.NotificationPreferencesJson = JsonSerializer.Serialize(preferences);
        await _context.SaveChangesAsync();
    }

    public bool ShouldNotify(NotificationPreferences preferences, NotificationCategory category)
    {
        return category switch
        {
            NotificationCategory.Battle => preferences.Battle,
            NotificationCategory.Guild => preferences.Guild,
            NotificationCategory.Progression => preferences.Progression,
            NotificationCategory.Marketplace => preferences.Marketplace,
            NotificationCategory.System => true,
            NotificationCategory.Security => true,
            _ => true
        };
    }

    private static NotificationCategory GetCategory(NotificationType type)
    {
        return type switch
        {
            NotificationType.BattleCompleted or NotificationType.WinStreakMilestone or NotificationType.RatingMilestone
                => NotificationCategory.Battle,

            NotificationType.GuildInvited or NotificationType.GuildInviteResponse or NotificationType.GuildKicked
                or NotificationType.GuildPromoted or NotificationType.GuildBossSpawned or NotificationType.GuildBossDefeated
                or NotificationType.GuildChatMention or NotificationType.GuildStrategyPublished or NotificationType.GuildTreasuryUpgrade
                => NotificationCategory.Guild,

            NotificationType.LevelUp or NotificationType.AchievementUnlocked or NotificationType.DailyChallengesAvailable
                or NotificationType.MasteryRankUp
                => NotificationCategory.Progression,

            NotificationType.StrategyRated or NotificationType.StrategyDownloadMilestone
                => NotificationCategory.Marketplace,

            NotificationType.RevengeOpportunity or NotificationType.RivalDefeatedYou
                or NotificationType.TournamentResult or NotificationType.GuildWarResult
                or NotificationType.RankUp or NotificationType.RankDown
                => NotificationCategory.Battle,

            NotificationType.NewModifierActive or NotificationType.AdminAnnouncement
                => NotificationCategory.System,

            NotificationType.ApiKeyNewIp or NotificationType.PasswordChanged or NotificationType.TierChanged
                or NotificationType.AdminActionOnAccount
                => NotificationCategory.Security,

            _ => NotificationCategory.System
        };
    }

    public async Task<NotificationDigestResponse> GetDigestAsync(Guid playerId)
    {
        var unreadNotifications = await _context.Notifications
            .Where(n => n.PlayerId == playerId && !n.IsRead && (n.ExpiresAt == null || n.ExpiresAt > DateTime.UtcNow))
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

        var categories = unreadNotifications
            .GroupBy(n => n.Category)
            .Select(g => new DigestCategory
            {
                Category = g.Key.ToString(),
                UnreadCount = g.Count(),
                LatestAt = g.Max(n => n.CreatedAt)
            })
            .OrderByDescending(c => c.LatestAt)
            .ToList();

        var highlights = unreadNotifications
            .Take(10)
            .Select(n => new DigestItem
            {
                Id = n.Id,
                Type = n.Type.ToString(),
                Title = n.Title,
                Message = n.Message,
                ActionUrl = n.ActionUrl,
                CreatedAt = n.CreatedAt
            })
            .ToList();

        return new NotificationDigestResponse
        {
            TotalUnread = unreadNotifications.Count,
            Categories = categories,
            RecentHighlights = highlights
        };
    }

    public async Task SendRevengeAlertAsync(Guid loserId, Guid winnerId, Guid battleId)
    {
        var winner = await _context.Players.FindAsync(winnerId);
        if (winner == null) return;

        await SendAsync(loserId, NotificationType.RevengeOpportunity,
            "Revenge Opportunity!",
            $"{winner.Username} just defeated you. Queue up and take your revenge!",
            $"/api/v1/battle/queue");
    }

    public async Task SendRankChangeAlertAsync(Guid playerId, int oldRating, int newRating)
    {
        // Only alert on significant rank changes (crossing 100-point thresholds)
        int oldBracket = oldRating / 100;
        int newBracket = newRating / 100;

        if (oldBracket == newBracket) return;

        if (newBracket > oldBracket)
        {
            await SendAsync(playerId, NotificationType.RankUp,
                $"Rating Milestone: {newBracket * 100}+",
                $"You've climbed to {newRating} rating! Keep pushing higher.",
                "/api/v1/leaderboard");
        }
        else
        {
            await SendAsync(playerId, NotificationType.RankDown,
                $"Rating Drop Below {(oldBracket) * 100}",
                $"Your rating dropped to {newRating}. Time to rally and climb back!",
                "/api/v1/battle/queue");
        }
    }

    private static NotificationPreferences DeserializePreferences(string? json)
    {
        if (string.IsNullOrEmpty(json))
            return new NotificationPreferences();

        try
        {
            return JsonSerializer.Deserialize<NotificationPreferences>(json) ?? new NotificationPreferences();
        }
        catch
        {
            return new NotificationPreferences();
        }
    }
}
