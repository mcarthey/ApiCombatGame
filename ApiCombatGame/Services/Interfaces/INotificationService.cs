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
}

public class NotificationPreferences
{
    public bool Battle { get; set; } = true;
    public bool Guild { get; set; } = true;
    public bool Progression { get; set; } = true;
    public bool Marketplace { get; set; } = true;
    // System and Security are always on, not configurable
}
