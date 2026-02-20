using ApiCombatGame.Data;
using ApiCombatGame.Models.Domain;
using ApiCombatGame.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ApiCombatGame.Services;

public class GuildChatService : IGuildChatService
{
    private readonly GameDbContext _context;
    private readonly INotificationService _notifications;
    private static readonly System.Text.RegularExpressions.Regex MentionPattern =
        new(@"@(\w{1,32})", System.Text.RegularExpressions.RegexOptions.Compiled);

    public GuildChatService(GameDbContext context, INotificationService notifications)
    {
        _context = context;
        _notifications = notifications;
    }

    public async Task<List<GuildChatMessage>> GetMessagesAsync(Guid guildId, int limit = 50, Guid? beforeId = null)
    {
        var query = _context.GuildChatMessages
            .Include(m => m.Player)
            .Where(m => m.GuildId == guildId);

        if (beforeId.HasValue)
        {
            var beforeMsg = await _context.GuildChatMessages.FindAsync(beforeId.Value);
            if (beforeMsg != null)
                query = query.Where(m => m.CreatedAt < beforeMsg.CreatedAt);
        }

        return await query
            .OrderByDescending(m => m.CreatedAt)
            .Take(Math.Clamp(limit, 1, 100))
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();
    }

    public async Task<GuildChatMessage> PostMessageAsync(Guid guildId, Guid playerId, string message)
    {
        message = message?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(message))
            throw new InvalidOperationException("Message cannot be empty.");

        if (message.Length > 500)
            throw new InvalidOperationException("Message cannot exceed 500 characters.");

        var membership = await _context.GuildMemberships
            .AnyAsync(m => m.GuildId == guildId && m.PlayerId == playerId);
        if (!membership)
            throw new InvalidOperationException("You are not a member of this guild.");

        var chatMessage = new GuildChatMessage
        {
            Id = Guid.NewGuid(),
            GuildId = guildId,
            PlayerId = playerId,
            Message = message,
            MessageType = "chat",
            CreatedAt = DateTime.UtcNow
        };

        _context.GuildChatMessages.Add(chatMessage);
        await _context.SaveChangesAsync();

        // Reload with navigation properties
        await _context.Entry(chatMessage).Reference(m => m.Player).LoadAsync();

        // Detect @mentions and notify mentioned players
        var matches = MentionPattern.Matches(message);
        if (matches.Count > 0)
        {
            var senderName = chatMessage.Player?.Username ?? "Someone";
            var mentionedUsernames = matches.Select(m => m.Groups[1].Value).Distinct().ToList();
            var mentionedPlayers = await _context.Players
                .Where(p => mentionedUsernames.Contains(p.Username))
                .Select(p => new { p.Id, p.Username })
                .ToListAsync();

            foreach (var mentioned in mentionedPlayers)
            {
                if (mentioned.Id != playerId) // Don't notify yourself
                {
                    await _notifications.SendAsync(mentioned.Id, Models.Enums.NotificationType.GuildChatMention, "Mentioned in Chat",
                        $"{senderName} mentioned you in guild chat: \"{(message.Length > 100 ? message[..100] + "..." : message)}\"");
                }
            }
        }

        return chatMessage;
    }

    public async Task PostSystemMessageAsync(Guid guildId, string message)
    {
        var chatMessage = new GuildChatMessage
        {
            Id = Guid.NewGuid(),
            GuildId = guildId,
            PlayerId = null,
            Message = message,
            MessageType = "system",
            CreatedAt = DateTime.UtcNow
        };

        _context.GuildChatMessages.Add(chatMessage);
        await _context.SaveChangesAsync();
    }
}
