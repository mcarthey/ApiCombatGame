using ApiCombatGame.Data;
using ApiCombatGame.Models.Domain;
using ApiCombatGame.Models.Enums;
using ApiCombatGame.Services;
using ApiCombatGame.Services.Interfaces;
using ApiCombatGame.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ApiCombatGame.Tests.UnitTests;

public class NotificationServiceTests
{
    private static (NotificationService Service, GameDbContext Context) CreateService()
    {
        var context = TestDbContextFactory.Create();
        var logger = NullLogger<NotificationService>.Instance;
        var service = new NotificationService(context, logger);
        return (service, context);
    }

    // ──────────────────────────────────────────────
    // SendAsync
    // ──────────────────────────────────────────────

    [Fact]
    public async Task SendAsync_CreatesNotification()
    {
        var (service, context) = CreateService();
        var player = TestDbContextFactory.CreatePlayer(context, "sender");

        await service.SendAsync(player.Id, NotificationType.BattleCompleted,
            "Battle Won", "You defeated your opponent!", "/battles/123");

        var notifications = context.Notifications.Where(n => n.PlayerId == player.Id).ToList();
        Assert.Single(notifications);

        var notification = notifications[0];
        Assert.Equal(NotificationType.BattleCompleted, notification.Type);
        Assert.Equal(NotificationCategory.Battle, notification.Category);
        Assert.Equal("Battle Won", notification.Title);
        Assert.Equal("You defeated your opponent!", notification.Message);
        Assert.Equal("/battles/123", notification.ActionUrl);
        Assert.False(notification.IsRead);
        Assert.NotNull(notification.ExpiresAt);
    }

    [Fact]
    public async Task SendAsync_RespectsBattlePreference_DisabledMeansNoNotification()
    {
        var (service, context) = CreateService();
        var player = TestDbContextFactory.CreatePlayer(context, "quiet_player");

        // Disable battle notifications
        await service.UpdatePreferencesAsync(player.Id, new NotificationPreferences
        {
            Battle = false,
            Guild = true,
            Progression = true,
            Marketplace = true
        });

        await service.SendAsync(player.Id, NotificationType.BattleCompleted,
            "Battle Done", "You won!");

        var count = context.Notifications.Count(n => n.PlayerId == player.Id);
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task SendAsync_AlwaysSendsSecurityCategory_RegardlessOfPreferences()
    {
        var (service, context) = CreateService();
        var player = TestDbContextFactory.CreatePlayer(context, "secure_player");

        // Disable everything possible
        await service.UpdatePreferencesAsync(player.Id, new NotificationPreferences
        {
            Battle = false,
            Guild = false,
            Progression = false,
            Marketplace = false
        });

        await service.SendAsync(player.Id, NotificationType.PasswordChanged,
            "Password Changed", "Your password was changed.");

        var notifications = context.Notifications.Where(n => n.PlayerId == player.Id).ToList();
        Assert.Single(notifications);
        Assert.Equal(NotificationCategory.Security, notifications[0].Category);
    }

    [Fact]
    public async Task SendAsync_AlwaysSendsSystemCategory_RegardlessOfPreferences()
    {
        var (service, context) = CreateService();
        var player = TestDbContextFactory.CreatePlayer(context, "system_player");

        // Disable everything possible
        await service.UpdatePreferencesAsync(player.Id, new NotificationPreferences
        {
            Battle = false,
            Guild = false,
            Progression = false,
            Marketplace = false
        });

        await service.SendAsync(player.Id, NotificationType.AdminAnnouncement,
            "Maintenance", "Server maintenance tonight.");

        var notifications = context.Notifications.Where(n => n.PlayerId == player.Id).ToList();
        Assert.Single(notifications);
        Assert.Equal(NotificationCategory.System, notifications[0].Category);
    }

    // ──────────────────────────────────────────────
    // SendToGuildAsync
    // ──────────────────────────────────────────────

    [Fact]
    public async Task SendToGuildAsync_SendsToAllMembers()
    {
        var (service, context) = CreateService();
        var leader = TestDbContextFactory.CreatePlayer(context, "guild_leader");
        var member1 = TestDbContextFactory.CreatePlayer(context, "guild_member1");
        var member2 = TestDbContextFactory.CreatePlayer(context, "guild_member2");

        var (guild, _) = TestDbContextFactory.CreateGuildWithLeader(context, leader, "Notify Guild", "NTFY");
        TestDbContextFactory.AddGuildMember(context, guild.Id, member1);
        TestDbContextFactory.AddGuildMember(context, guild.Id, member2);

        await service.SendToGuildAsync(guild.Id, NotificationType.GuildBossSpawned,
            "Boss Spawned", "A guild boss has appeared!");

        var notifications = context.Notifications.ToList();
        Assert.Equal(3, notifications.Count);

        var playerIds = notifications.Select(n => n.PlayerId).Distinct().ToList();
        Assert.Contains(leader.Id, playerIds);
        Assert.Contains(member1.Id, playerIds);
        Assert.Contains(member2.Id, playerIds);
    }

    [Fact]
    public async Task SendToGuildAsync_ExcludesSpecifiedPlayer()
    {
        var (service, context) = CreateService();
        var leader = TestDbContextFactory.CreatePlayer(context, "leader_excl");
        var member = TestDbContextFactory.CreatePlayer(context, "member_excl");

        var (guild, _) = TestDbContextFactory.CreateGuildWithLeader(context, leader, "Excl Guild", "EXCL");
        TestDbContextFactory.AddGuildMember(context, guild.Id, member);

        await service.SendToGuildAsync(guild.Id, NotificationType.GuildPromoted,
            "Promotion", "A member was promoted.", excludePlayerId: leader.Id);

        var notifications = context.Notifications.ToList();
        Assert.Single(notifications);
        Assert.Equal(member.Id, notifications[0].PlayerId);
    }

    [Fact]
    public async Task SendToGuildAsync_RespectsPerMemberPreferences()
    {
        var (service, context) = CreateService();
        var leader = TestDbContextFactory.CreatePlayer(context, "pref_leader");
        var memberWithGuildOff = TestDbContextFactory.CreatePlayer(context, "pref_off_member");
        var memberWithGuildOn = TestDbContextFactory.CreatePlayer(context, "pref_on_member");

        var (guild, _) = TestDbContextFactory.CreateGuildWithLeader(context, leader, "Pref Guild", "PREF");
        TestDbContextFactory.AddGuildMember(context, guild.Id, memberWithGuildOff);
        TestDbContextFactory.AddGuildMember(context, guild.Id, memberWithGuildOn);

        // Disable guild notifications for one member
        await service.UpdatePreferencesAsync(memberWithGuildOff.Id, new NotificationPreferences
        {
            Battle = true,
            Guild = false,
            Progression = true,
            Marketplace = true
        });

        await service.SendToGuildAsync(guild.Id, NotificationType.GuildBossDefeated,
            "Boss Defeated", "The guild boss was defeated!");

        var notifications = context.Notifications.ToList();

        // Leader and memberWithGuildOn should receive, memberWithGuildOff should not
        Assert.Equal(2, notifications.Count);

        var recipientIds = notifications.Select(n => n.PlayerId).ToList();
        Assert.Contains(leader.Id, recipientIds);
        Assert.Contains(memberWithGuildOn.Id, recipientIds);
        Assert.DoesNotContain(memberWithGuildOff.Id, recipientIds);
    }

    // ──────────────────────────────────────────────
    // GetUnreadCountAsync
    // ──────────────────────────────────────────────

    [Fact]
    public async Task GetUnreadCountAsync_ReturnsCorrectCount()
    {
        var (service, context) = CreateService();
        var player = TestDbContextFactory.CreatePlayer(context, "count_player");

        // Add 3 unread notifications
        for (int i = 0; i < 3; i++)
        {
            await service.SendAsync(player.Id, NotificationType.LevelUp,
                $"Level Up {i}", $"You reached level {i + 2}!");
        }

        var count = await service.GetUnreadCountAsync(player.Id);
        Assert.Equal(3, count);
    }

    [Fact]
    public async Task GetUnreadCountAsync_ExcludesReadNotifications()
    {
        var (service, context) = CreateService();
        var player = TestDbContextFactory.CreatePlayer(context, "read_count");

        await service.SendAsync(player.Id, NotificationType.LevelUp, "Level 2", "Congrats!");
        await service.SendAsync(player.Id, NotificationType.LevelUp, "Level 3", "Congrats again!");

        // Mark one as read
        var firstNotification = context.Notifications.First(n => n.PlayerId == player.Id);
        await service.MarkReadAsync(firstNotification.Id, player.Id);

        var count = await service.GetUnreadCountAsync(player.Id);
        Assert.Equal(1, count);
    }

    // ──────────────────────────────────────────────
    // GetNotificationsAsync
    // ──────────────────────────────────────────────

    [Fact]
    public async Task GetNotificationsAsync_PaginatesCorrectly()
    {
        var (service, context) = CreateService();
        var player = TestDbContextFactory.CreatePlayer(context, "paginate_player");

        // Create 5 notifications with staggered times so ordering is deterministic
        for (int i = 0; i < 5; i++)
        {
            context.Notifications.Add(new Notification
            {
                Id = Guid.NewGuid(),
                PlayerId = player.Id,
                Type = NotificationType.AchievementUnlocked,
                Category = NotificationCategory.Progression,
                Title = $"Achievement {i}",
                Message = $"You unlocked achievement {i}",
                CreatedAt = DateTime.UtcNow.AddMinutes(i),
                ExpiresAt = DateTime.UtcNow.AddDays(30)
            });
        }
        await context.SaveChangesAsync();

        // Page 1, size 2 (should get the 2 most recent by CreatedAt desc)
        var page1 = await service.GetNotificationsAsync(player.Id, page: 1, pageSize: 2);
        Assert.Equal(2, page1.Count);
        Assert.Equal("Achievement 4", page1[0].Title);
        Assert.Equal("Achievement 3", page1[1].Title);

        // Page 2, size 2
        var page2 = await service.GetNotificationsAsync(player.Id, page: 2, pageSize: 2);
        Assert.Equal(2, page2.Count);
        Assert.Equal("Achievement 2", page2[0].Title);
        Assert.Equal("Achievement 1", page2[1].Title);

        // Page 3, size 2 (only 1 remaining)
        var page3 = await service.GetNotificationsAsync(player.Id, page: 3, pageSize: 2);
        Assert.Single(page3);
        Assert.Equal("Achievement 0", page3[0].Title);
    }

    [Fact]
    public async Task GetNotificationsAsync_UnreadOnlyFilterWorks()
    {
        var (service, context) = CreateService();
        var player = TestDbContextFactory.CreatePlayer(context, "filter_player");

        await service.SendAsync(player.Id, NotificationType.LevelUp, "Unread 1", "Message 1");
        await service.SendAsync(player.Id, NotificationType.LevelUp, "Unread 2", "Message 2");
        await service.SendAsync(player.Id, NotificationType.LevelUp, "Will be read", "Message 3");

        // Mark the last one as read
        var toRead = context.Notifications.First(n => n.Title == "Will be read");
        await service.MarkReadAsync(toRead.Id, player.Id);

        var unreadOnly = await service.GetNotificationsAsync(player.Id, unreadOnly: true);
        Assert.Equal(2, unreadOnly.Count);
        Assert.All(unreadOnly, n => Assert.False(n.IsRead));

        var all = await service.GetNotificationsAsync(player.Id, unreadOnly: false);
        Assert.Equal(3, all.Count);
    }

    // ──────────────────────────────────────────────
    // MarkReadAsync / MarkAllReadAsync
    // ──────────────────────────────────────────────

    [Fact]
    public async Task MarkReadAsync_MarksSpecificNotification()
    {
        var (service, context) = CreateService();
        var player = TestDbContextFactory.CreatePlayer(context, "mark_player");

        await service.SendAsync(player.Id, NotificationType.BattleCompleted, "Battle 1", "You won!");
        await service.SendAsync(player.Id, NotificationType.BattleCompleted, "Battle 2", "You lost!");

        var first = context.Notifications.First(n => n.Title == "Battle 1");
        await service.MarkReadAsync(first.Id, player.Id);

        // Reload to verify
        var reloaded = context.Notifications.First(n => n.Id == first.Id);
        Assert.True(reloaded.IsRead);

        // The other should remain unread
        var second = context.Notifications.First(n => n.Title == "Battle 2");
        Assert.False(second.IsRead);
    }

    [Fact]
    public async Task MarkAllReadAsync_MarksAllUnreadNotifications()
    {
        var (service, context) = CreateService();
        var player = TestDbContextFactory.CreatePlayer(context, "markall_player");

        await service.SendAsync(player.Id, NotificationType.LevelUp, "Level 2", "Msg 1");
        await service.SendAsync(player.Id, NotificationType.LevelUp, "Level 3", "Msg 2");
        await service.SendAsync(player.Id, NotificationType.LevelUp, "Level 4", "Msg 3");

        await service.MarkAllReadAsync(player.Id);

        var allNotifications = context.Notifications.Where(n => n.PlayerId == player.Id).ToList();
        Assert.Equal(3, allNotifications.Count);
        Assert.All(allNotifications, n => Assert.True(n.IsRead));
    }

    // ──────────────────────────────────────────────
    // DeleteExpiredAsync
    // ──────────────────────────────────────────────

    [Fact]
    public async Task DeleteExpiredAsync_RemovesExpiredNotifications()
    {
        var (service, context) = CreateService();
        var player = TestDbContextFactory.CreatePlayer(context, "expire_player");

        // Add an expired notification
        context.Notifications.Add(new Notification
        {
            Id = Guid.NewGuid(),
            PlayerId = player.Id,
            Type = NotificationType.BattleCompleted,
            Category = NotificationCategory.Battle,
            Title = "Old Battle",
            Message = "This expired",
            CreatedAt = DateTime.UtcNow.AddDays(-31),
            ExpiresAt = DateTime.UtcNow.AddDays(-1) // already expired
        });

        // Add a non-expired notification
        context.Notifications.Add(new Notification
        {
            Id = Guid.NewGuid(),
            PlayerId = player.Id,
            Type = NotificationType.BattleCompleted,
            Category = NotificationCategory.Battle,
            Title = "Recent Battle",
            Message = "This is fresh",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(29)
        });
        await context.SaveChangesAsync();

        await service.DeleteExpiredAsync();

        var remaining = context.Notifications.Where(n => n.PlayerId == player.Id).ToList();
        Assert.Single(remaining);
        Assert.Equal("Recent Battle", remaining[0].Title);
    }

    [Fact]
    public async Task DeleteExpiredAsync_RemovesOldReadNotifications()
    {
        var (service, context) = CreateService();
        var player = TestDbContextFactory.CreatePlayer(context, "oldread_player");

        // Old read notification (older than 7 days)
        context.Notifications.Add(new Notification
        {
            Id = Guid.NewGuid(),
            PlayerId = player.Id,
            Type = NotificationType.LevelUp,
            Category = NotificationCategory.Progression,
            Title = "Old Read",
            Message = "Read 10 days ago",
            IsRead = true,
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            ExpiresAt = DateTime.UtcNow.AddDays(20)
        });

        // Recent read notification (within 7 days)
        context.Notifications.Add(new Notification
        {
            Id = Guid.NewGuid(),
            PlayerId = player.Id,
            Type = NotificationType.LevelUp,
            Category = NotificationCategory.Progression,
            Title = "Recent Read",
            Message = "Read 2 days ago",
            IsRead = true,
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            ExpiresAt = DateTime.UtcNow.AddDays(28)
        });

        // Unread notification (should survive even if old)
        context.Notifications.Add(new Notification
        {
            Id = Guid.NewGuid(),
            PlayerId = player.Id,
            Type = NotificationType.LevelUp,
            Category = NotificationCategory.Progression,
            Title = "Old Unread",
            Message = "Never read but old",
            IsRead = false,
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            ExpiresAt = DateTime.UtcNow.AddDays(20)
        });
        await context.SaveChangesAsync();

        await service.DeleteExpiredAsync();

        var remaining = context.Notifications.Where(n => n.PlayerId == player.Id).ToList();
        Assert.Equal(2, remaining.Count);

        var titles = remaining.Select(n => n.Title).ToList();
        Assert.Contains("Recent Read", titles);
        Assert.Contains("Old Unread", titles);
        Assert.DoesNotContain("Old Read", titles);
    }

    // ──────────────────────────────────────────────
    // Preferences round-trip
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Preferences_RoundTrip_GetUpdateGet()
    {
        var (service, context) = CreateService();
        var player = TestDbContextFactory.CreatePlayer(context, "prefs_player");

        // Default preferences (all true)
        var defaults = await service.GetPreferencesAsync(player.Id);
        Assert.True(defaults.Battle);
        Assert.True(defaults.Guild);
        Assert.True(defaults.Progression);
        Assert.True(defaults.Marketplace);

        // Update preferences
        var updated = new NotificationPreferences
        {
            Battle = false,
            Guild = true,
            Progression = false,
            Marketplace = true
        };
        await service.UpdatePreferencesAsync(player.Id, updated);

        // Read back
        var retrieved = await service.GetPreferencesAsync(player.Id);
        Assert.False(retrieved.Battle);
        Assert.True(retrieved.Guild);
        Assert.False(retrieved.Progression);
        Assert.True(retrieved.Marketplace);
    }

    [Fact]
    public async Task GetPreferencesAsync_ReturnsDefaultsForNewPlayer()
    {
        var (service, context) = CreateService();
        var player = TestDbContextFactory.CreatePlayer(context, "new_player");

        // Player has no NotificationPreferencesJson stored yet
        Assert.Null(player.NotificationPreferencesJson);

        var prefs = await service.GetPreferencesAsync(player.Id);

        Assert.True(prefs.Battle);
        Assert.True(prefs.Guild);
        Assert.True(prefs.Progression);
        Assert.True(prefs.Marketplace);
    }

    // ──────────────────────────────────────────────
    // ShouldNotify
    // ──────────────────────────────────────────────

    [Fact]
    public void ShouldNotify_ReturnsCorrectValuesPerCategory()
    {
        var (service, _) = CreateService();

        var prefs = new NotificationPreferences
        {
            Battle = true,
            Guild = false,
            Progression = true,
            Marketplace = false
        };

        Assert.True(service.ShouldNotify(prefs, NotificationCategory.Battle));
        Assert.False(service.ShouldNotify(prefs, NotificationCategory.Guild));
        Assert.True(service.ShouldNotify(prefs, NotificationCategory.Progression));
        Assert.False(service.ShouldNotify(prefs, NotificationCategory.Marketplace));

        // System and Security are always true regardless of preferences
        Assert.True(service.ShouldNotify(prefs, NotificationCategory.System));
        Assert.True(service.ShouldNotify(prefs, NotificationCategory.Security));
    }
}
