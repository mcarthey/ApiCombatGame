using ApiCombatGame.Models.Domain;
using ApiCombatGame.Models.Enums;

namespace ApiCombatGame.Services.Interfaces;

public interface INotificationService
{
    Task SendAsync(Guid playerId, NotificationType type, string title, string message, string? actionUrl = null);
    Task SendToGuildAsync(Guid guildId, NotificationType type, string title, string message, string? actionUrl = null, Guid? excludePlayerId = null);
    Task<int> GetUnreadCountAsync(Guid playerId);
    Task<List<Notification>> GetNotificationsAsync(Guid playerId, int page = 1, int pageSize = 20, bool unreadOnly = false);
    Task MarkReadAsync(Guid notificationId, Guid playerId);
    Task MarkAllReadAsync(Guid playerId);
    Task DeleteExpiredAsync();
    Task<NotificationPreferences> GetPreferencesAsync(Guid playerId);
    Task UpdatePreferencesAsync(Guid playerId, NotificationPreferences preferences);
    bool ShouldNotify(NotificationPreferences preferences, NotificationCategory category);
    Task<NotificationDigestResponse> GetDigestAsync(Guid playerId);
    Task SendRevengeAlertAsync(Guid loserId, Guid winnerId, Guid battleId);
    Task SendRankChangeAlertAsync(Guid playerId, int oldRating, int newRating);
}

public class NotificationPreferences
{
    public bool Battle { get; set; } = true;
    public bool Guild { get; set; } = true;
    public bool Progression { get; set; } = true;
    public bool Marketplace { get; set; } = true;
    public bool Competitive { get; set; } = true;
    // System and Security are always on, not configurable
}

/// <summary>Summarized notification digest grouped by category.</summary>
public class NotificationDigestResponse
{
    public int TotalUnread { get; set; }
    public List<DigestCategory> Categories { get; set; } = new();
    public List<DigestItem> RecentHighlights { get; set; } = new();
}

public class DigestCategory
{
    public string Category { get; set; } = string.Empty;
    public int UnreadCount { get; set; }
    public DateTime? LatestAt { get; set; }
}

public class DigestItem
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? ActionUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}
