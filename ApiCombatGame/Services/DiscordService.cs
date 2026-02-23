using ApiCombatGame.Data;
using ApiCombatGame.Models.Domain;
using ApiCombatGame.Models.DTOs.Discord;
using ApiCombatGame.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ApiCombatGame.Services;

public class DiscordService : IDiscordService
{
    private readonly GameDbContext _context;
    private readonly ILogger<DiscordService> _logger;

    public DiscordService(GameDbContext context, ILogger<DiscordService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<DiscordLinkResponse> LinkAccountAsync(Guid playerId, LinkDiscordRequest request)
    {
        var player = await _context.Players.FindAsync(playerId)
            ?? throw new KeyNotFoundException("Player not found.");

        var existing = await _context.Set<DiscordLink>()
            .FirstOrDefaultAsync(d => d.PlayerId == playerId);

        if (existing != null)
            throw new InvalidOperationException("Account already linked to Discord. Unlink first to relink.");

        var dupeCheck = await _context.Set<DiscordLink>()
            .AnyAsync(d => d.DiscordUserId == request.DiscordUserId && d.IsVerified);

        if (dupeCheck)
            throw new InvalidOperationException("This Discord account is already linked to another player.");

        var code = GenerateVerificationCode();

        var link = new DiscordLink
        {
            Id = Guid.NewGuid(),
            PlayerId = playerId,
            DiscordUserId = request.DiscordUserId,
            DiscordUsername = request.DiscordUsername,
            VerificationCode = code,
            IsVerified = false
        };

        _context.Set<DiscordLink>().Add(link);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Player {PlayerId} initiated Discord link for {DiscordUserId}", playerId, request.DiscordUserId);

        return new DiscordLinkResponse
        {
            DiscordUserId = link.DiscordUserId,
            DiscordUsername = link.DiscordUsername,
            IsVerified = false,
            NotificationsEnabled = link.NotificationsEnabled,
            VerificationCode = code,
            LinkedAt = link.LinkedAt
        };
    }

    public async Task<DiscordLinkResponse> VerifyLinkAsync(Guid playerId, string verificationCode)
    {
        var link = await _context.Set<DiscordLink>()
            .FirstOrDefaultAsync(d => d.PlayerId == playerId)
            ?? throw new KeyNotFoundException("No Discord link found. Link your account first.");

        if (link.IsVerified)
            throw new InvalidOperationException("Discord link already verified.");

        if (link.VerificationCode != verificationCode)
            throw new InvalidOperationException("Invalid verification code.");

        link.IsVerified = true;
        link.VerificationCode = null;
        await _context.SaveChangesAsync();

        return new DiscordLinkResponse
        {
            DiscordUserId = link.DiscordUserId,
            DiscordUsername = link.DiscordUsername,
            IsVerified = true,
            NotificationsEnabled = link.NotificationsEnabled,
            LinkedAt = link.LinkedAt
        };
    }

    public async Task<DiscordLinkResponse?> GetLinkAsync(Guid playerId)
    {
        var link = await _context.Set<DiscordLink>()
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.PlayerId == playerId);

        if (link == null) return null;

        return new DiscordLinkResponse
        {
            DiscordUserId = link.DiscordUserId,
            DiscordUsername = link.DiscordUsername,
            IsVerified = link.IsVerified,
            NotificationsEnabled = link.NotificationsEnabled,
            LinkedAt = link.LinkedAt
        };
    }

    public async Task UnlinkAsync(Guid playerId)
    {
        var link = await _context.Set<DiscordLink>()
            .FirstOrDefaultAsync(d => d.PlayerId == playerId)
            ?? throw new KeyNotFoundException("No Discord link found.");

        _context.Set<DiscordLink>().Remove(link);

        // Also remove webhooks
        var webhooks = await _context.Set<DiscordWebhook>()
            .Where(w => w.PlayerId == playerId)
            .ToListAsync();
        _context.Set<DiscordWebhook>().RemoveRange(webhooks);

        await _context.SaveChangesAsync();
    }

    public async Task<WebhookResponse> RegisterWebhookAsync(Guid playerId, RegisterWebhookRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.WebhookUrl))
            throw new InvalidOperationException("Webhook URL is required.");

        var webhookCount = await _context.Set<DiscordWebhook>()
            .CountAsync(w => w.PlayerId == playerId);

        if (webhookCount >= 5)
            throw new InvalidOperationException("Maximum 5 webhooks per player.");

        var webhook = new DiscordWebhook
        {
            Id = Guid.NewGuid(),
            PlayerId = playerId,
            WebhookUrl = request.WebhookUrl,
            Label = request.Label,
            EventTypes = string.Join(",", request.EventTypes)
        };

        _context.Set<DiscordWebhook>().Add(webhook);
        await _context.SaveChangesAsync();

        return new WebhookResponse
        {
            Id = webhook.Id,
            Label = webhook.Label,
            EventTypes = webhook.EventTypes,
            IsActive = webhook.IsActive,
            CreatedAt = webhook.CreatedAt
        };
    }

    public async Task<List<WebhookResponse>> GetWebhooksAsync(Guid playerId)
    {
        return await _context.Set<DiscordWebhook>()
            .Where(w => w.PlayerId == playerId)
            .Select(w => new WebhookResponse
            {
                Id = w.Id,
                Label = w.Label,
                EventTypes = w.EventTypes,
                IsActive = w.IsActive,
                CreatedAt = w.CreatedAt
            })
            .ToListAsync();
    }

    public async Task DeleteWebhookAsync(Guid playerId, Guid webhookId)
    {
        var webhook = await _context.Set<DiscordWebhook>()
            .FirstOrDefaultAsync(w => w.Id == webhookId && w.PlayerId == playerId)
            ?? throw new KeyNotFoundException("Webhook not found.");

        _context.Set<DiscordWebhook>().Remove(webhook);
        await _context.SaveChangesAsync();
    }

    public async Task<DiscordProfileResponse?> GetProfileByDiscordIdAsync(string discordUserId)
    {
        var link = await _context.Set<DiscordLink>()
            .AsNoTracking()
            .Include(d => d.Player)
            .FirstOrDefaultAsync(d => d.DiscordUserId == discordUserId && d.IsVerified);

        if (link == null) return null;

        var guild = await _context.GuildMemberships
            .AsNoTracking()
            .Include(m => m.Guild)
            .FirstOrDefaultAsync(m => m.PlayerId == link.PlayerId);

        return new DiscordProfileResponse
        {
            Username = link.Player.Username,
            Level = link.Player.Level,
            Rating = link.Player.Rating,
            WinStreak = link.Player.WinStreak,
            CurrentTier = link.Player.CurrentTier.ToString(),
            Badge = link.Player.Badge,
            GuildName = guild?.Guild?.Name
        };
    }

    private static string GenerateVerificationCode()
    {
        return new Random().Next(100000, 999999).ToString();
    }
}
