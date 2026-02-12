using ApiCombatGame.Data;
using ApiCombatGame.Models.Domain;
using ApiCombatGame.Models.Enums;
using ApiCombatGame.Services;
using ApiCombatGame.Services.Interfaces;
using ApiCombatGame.Tests.Helpers;
using Moq;
using Xunit;

namespace ApiCombatGame.Tests.UnitTests;

public class GuildChatServiceTests
{
    private readonly Mock<INotificationService> _notificationsMock = new();

    private (GameDbContext context, GuildChatService service) CreateService()
    {
        var context = TestDbContextFactory.Create();
        var service = new GuildChatService(context, _notificationsMock.Object);
        return (context, service);
    }

    // ─── PostMessageAsync ───────────────────────────────────────────────

    [Fact]
    public async Task PostMessage_CreatesChatMessage()
    {
        var (context, service) = CreateService();
        var leader = TestDbContextFactory.CreatePlayer(context, "leader");
        var (guild, _) = TestDbContextFactory.CreateGuildWithLeader(context, leader);

        var result = await service.PostMessageAsync(guild.Id, leader.Id, "Hello guild!");

        Assert.NotNull(result);
        Assert.Equal(guild.Id, result.GuildId);
        Assert.Equal(leader.Id, result.PlayerId);
        Assert.Equal("Hello guild!", result.Message);
        Assert.Equal("chat", result.MessageType);
    }

    [Fact]
    public async Task PostMessage_FailsForNonMember()
    {
        var (context, service) = CreateService();
        var leader = TestDbContextFactory.CreatePlayer(context, "leader");
        var outsider = TestDbContextFactory.CreatePlayer(context, "outsider");
        var (guild, _) = TestDbContextFactory.CreateGuildWithLeader(context, leader);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.PostMessageAsync(guild.Id, outsider.Id, "Hello!"));

        Assert.Contains("not a member", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PostMessage_FailsForEmptyMessage()
    {
        var (context, service) = CreateService();
        var leader = TestDbContextFactory.CreatePlayer(context, "leader");
        var (guild, _) = TestDbContextFactory.CreateGuildWithLeader(context, leader);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.PostMessageAsync(guild.Id, leader.Id, ""));
    }

    [Fact]
    public async Task PostMessage_FailsForWhitespaceOnlyMessage()
    {
        var (context, service) = CreateService();
        var leader = TestDbContextFactory.CreatePlayer(context, "leader");
        var (guild, _) = TestDbContextFactory.CreateGuildWithLeader(context, leader);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.PostMessageAsync(guild.Id, leader.Id, "   "));
    }

    [Fact]
    public async Task PostMessage_FailsForMessageExceeding500Characters()
    {
        var (context, service) = CreateService();
        var leader = TestDbContextFactory.CreatePlayer(context, "leader");
        var (guild, _) = TestDbContextFactory.CreateGuildWithLeader(context, leader);

        var longMessage = new string('A', 501);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.PostMessageAsync(guild.Id, leader.Id, longMessage));

        Assert.Contains("500", ex.Message);
    }

    [Fact]
    public async Task PostMessage_Accepts500CharacterMessage()
    {
        var (context, service) = CreateService();
        var leader = TestDbContextFactory.CreatePlayer(context, "leader");
        var (guild, _) = TestDbContextFactory.CreateGuildWithLeader(context, leader);

        var exactMessage = new string('B', 500);

        var result = await service.PostMessageAsync(guild.Id, leader.Id, exactMessage);

        Assert.Equal(500, result.Message.Length);
    }

    [Fact]
    public async Task PostMessage_TrimsWhitespace()
    {
        var (context, service) = CreateService();
        var leader = TestDbContextFactory.CreatePlayer(context, "leader");
        var (guild, _) = TestDbContextFactory.CreateGuildWithLeader(context, leader);

        var result = await service.PostMessageAsync(guild.Id, leader.Id, "  Hello!  ");

        Assert.Equal("Hello!", result.Message);
    }

    [Fact]
    public async Task PostMessage_DetectsMentionAndSendsNotification()
    {
        var (context, service) = CreateService();
        var leader = TestDbContextFactory.CreatePlayer(context, "leader");
        var mentioned = TestDbContextFactory.CreatePlayer(context, "targetuser");
        var (guild, _) = TestDbContextFactory.CreateGuildWithLeader(context, leader);
        TestDbContextFactory.AddGuildMember(context, guild.Id, mentioned);

        _notificationsMock
            .Setup(n => n.SendAsync(
                It.IsAny<Guid>(),
                It.IsAny<NotificationType>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        await service.PostMessageAsync(guild.Id, leader.Id, "Hey @targetuser check this out!");

        _notificationsMock.Verify(n => n.SendAsync(
            mentioned.Id,
            NotificationType.GuildChatMention,
            It.IsAny<string>(),
            It.Is<string>(s => s.Contains("leader")),
            It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task PostMessage_DoesNotSelfNotifyOnMention()
    {
        var (context, service) = CreateService();
        var leader = TestDbContextFactory.CreatePlayer(context, "leader");
        var (guild, _) = TestDbContextFactory.CreateGuildWithLeader(context, leader);

        _notificationsMock
            .Setup(n => n.SendAsync(
                It.IsAny<Guid>(),
                It.IsAny<NotificationType>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        await service.PostMessageAsync(guild.Id, leader.Id, "Talking to myself @leader");

        _notificationsMock.Verify(n => n.SendAsync(
            leader.Id,
            It.IsAny<NotificationType>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string?>()), Times.Never);
    }

    [Fact]
    public async Task PostMessage_DoesNotNotifyForUnknownMention()
    {
        var (context, service) = CreateService();
        var leader = TestDbContextFactory.CreatePlayer(context, "leader");
        var (guild, _) = TestDbContextFactory.CreateGuildWithLeader(context, leader);

        _notificationsMock
            .Setup(n => n.SendAsync(
                It.IsAny<Guid>(),
                It.IsAny<NotificationType>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        await service.PostMessageAsync(guild.Id, leader.Id, "Hey @nobodyhere where are you?");

        _notificationsMock.Verify(n => n.SendAsync(
            It.IsAny<Guid>(),
            It.IsAny<NotificationType>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string?>()), Times.Never);
    }

    // ─── PostSystemMessageAsync ─────────────────────────────────────────

    [Fact]
    public async Task PostSystemMessage_CreatesSystemMessageWithNullPlayerId()
    {
        var (context, service) = CreateService();
        var leader = TestDbContextFactory.CreatePlayer(context, "leader");
        var (guild, _) = TestDbContextFactory.CreateGuildWithLeader(context, leader);

        await service.PostSystemMessageAsync(guild.Id, "A new boss has appeared!");

        var messages = context.GuildChatMessages.Where(m => m.GuildId == guild.Id).ToList();
        Assert.Single(messages);

        var msg = messages.First();
        Assert.Null(msg.PlayerId);
        Assert.Equal("system", msg.MessageType);
        Assert.Equal("A new boss has appeared!", msg.Message);
    }

    // ─── GetMessagesAsync ───────────────────────────────────────────────

    [Fact]
    public async Task GetMessages_ReturnsMessagesOrderedByCreatedAt()
    {
        var (context, service) = CreateService();
        var leader = TestDbContextFactory.CreatePlayer(context, "leader");
        var (guild, _) = TestDbContextFactory.CreateGuildWithLeader(context, leader);

        // Seed three messages with distinct timestamps
        var baseTime = DateTime.UtcNow.AddMinutes(-10);
        for (int i = 0; i < 3; i++)
        {
            context.GuildChatMessages.Add(new GuildChatMessage
            {
                Id = Guid.NewGuid(),
                GuildId = guild.Id,
                PlayerId = leader.Id,
                Message = $"Message {i}",
                MessageType = "chat",
                CreatedAt = baseTime.AddMinutes(i)
            });
        }
        context.SaveChanges();

        var result = await service.GetMessagesAsync(guild.Id);

        Assert.Equal(3, result.Count);
        Assert.True(result[0].CreatedAt <= result[1].CreatedAt);
        Assert.True(result[1].CreatedAt <= result[2].CreatedAt);
        Assert.Equal("Message 0", result[0].Message);
        Assert.Equal("Message 2", result[2].Message);
    }

    [Fact]
    public async Task GetMessages_RespectsLimitParameter()
    {
        var (context, service) = CreateService();
        var leader = TestDbContextFactory.CreatePlayer(context, "leader");
        var (guild, _) = TestDbContextFactory.CreateGuildWithLeader(context, leader);

        var baseTime = DateTime.UtcNow.AddMinutes(-20);
        for (int i = 0; i < 10; i++)
        {
            context.GuildChatMessages.Add(new GuildChatMessage
            {
                Id = Guid.NewGuid(),
                GuildId = guild.Id,
                PlayerId = leader.Id,
                Message = $"Message {i}",
                MessageType = "chat",
                CreatedAt = baseTime.AddMinutes(i)
            });
        }
        context.SaveChanges();

        var result = await service.GetMessagesAsync(guild.Id, limit: 3);

        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task GetMessages_ClampsLimitToMaximum100()
    {
        var (context, service) = CreateService();
        var leader = TestDbContextFactory.CreatePlayer(context, "leader");
        var (guild, _) = TestDbContextFactory.CreateGuildWithLeader(context, leader);

        // Seed 5 messages; requesting 200 should still only return 5
        var baseTime = DateTime.UtcNow.AddMinutes(-10);
        for (int i = 0; i < 5; i++)
        {
            context.GuildChatMessages.Add(new GuildChatMessage
            {
                Id = Guid.NewGuid(),
                GuildId = guild.Id,
                PlayerId = leader.Id,
                Message = $"Message {i}",
                MessageType = "chat",
                CreatedAt = baseTime.AddMinutes(i)
            });
        }
        context.SaveChanges();

        // Limit 200 should be clamped to 100 (but we only have 5, so we get 5)
        var result = await service.GetMessagesAsync(guild.Id, limit: 200);

        Assert.Equal(5, result.Count);
    }

    [Fact]
    public async Task GetMessages_ClampsLimitToMinimum1()
    {
        var (context, service) = CreateService();
        var leader = TestDbContextFactory.CreatePlayer(context, "leader");
        var (guild, _) = TestDbContextFactory.CreateGuildWithLeader(context, leader);

        var baseTime = DateTime.UtcNow.AddMinutes(-10);
        for (int i = 0; i < 5; i++)
        {
            context.GuildChatMessages.Add(new GuildChatMessage
            {
                Id = Guid.NewGuid(),
                GuildId = guild.Id,
                PlayerId = leader.Id,
                Message = $"Message {i}",
                MessageType = "chat",
                CreatedAt = baseTime.AddMinutes(i)
            });
        }
        context.SaveChanges();

        // Limit 0 should be clamped to 1
        var result = await service.GetMessagesAsync(guild.Id, limit: 0);

        Assert.Single(result);
    }

    [Fact]
    public async Task GetMessages_ReturnsOnlyMessagesForSpecifiedGuild()
    {
        var (context, service) = CreateService();
        var leader1 = TestDbContextFactory.CreatePlayer(context, "leader1");
        var leader2 = TestDbContextFactory.CreatePlayer(context, "leader2");
        var (guild1, _) = TestDbContextFactory.CreateGuildWithLeader(context, leader1, "Guild One", "ONE");
        var (guild2, _) = TestDbContextFactory.CreateGuildWithLeader(context, leader2, "Guild Two", "TWO");

        context.GuildChatMessages.Add(new GuildChatMessage
        {
            Id = Guid.NewGuid(),
            GuildId = guild1.Id,
            PlayerId = leader1.Id,
            Message = "Guild 1 message",
            MessageType = "chat",
            CreatedAt = DateTime.UtcNow
        });
        context.GuildChatMessages.Add(new GuildChatMessage
        {
            Id = Guid.NewGuid(),
            GuildId = guild2.Id,
            PlayerId = leader2.Id,
            Message = "Guild 2 message",
            MessageType = "chat",
            CreatedAt = DateTime.UtcNow
        });
        context.SaveChanges();

        var result = await service.GetMessagesAsync(guild1.Id);

        Assert.Single(result);
        Assert.Equal("Guild 1 message", result[0].Message);
    }

    [Fact]
    public async Task GetMessages_BeforeIdFiltersPreviousMessages()
    {
        var (context, service) = CreateService();
        var leader = TestDbContextFactory.CreatePlayer(context, "leader");
        var (guild, _) = TestDbContextFactory.CreateGuildWithLeader(context, leader);

        var baseTime = DateTime.UtcNow.AddMinutes(-10);
        var messages = new List<GuildChatMessage>();
        for (int i = 0; i < 5; i++)
        {
            var msg = new GuildChatMessage
            {
                Id = Guid.NewGuid(),
                GuildId = guild.Id,
                PlayerId = leader.Id,
                Message = $"Message {i}",
                MessageType = "chat",
                CreatedAt = baseTime.AddMinutes(i)
            };
            messages.Add(msg);
            context.GuildChatMessages.Add(msg);
        }
        context.SaveChanges();

        // Get messages before the 3rd message (index 2), should return messages 0 and 1
        var result = await service.GetMessagesAsync(guild.Id, beforeId: messages[2].Id);

        Assert.Equal(2, result.Count);
        Assert.All(result, m => Assert.True(m.CreatedAt < messages[2].CreatedAt));
    }
}
