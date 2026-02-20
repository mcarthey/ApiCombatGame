using ApiCombatGame.Data;
using ApiCombatGame.Models.Domain;
using ApiCombatGame.Models.Enums;
using ApiCombatGame.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ApiCombatGame.Services;

public class GuildService : IGuildService
{
    private readonly GameDbContext _context;
    private readonly INotificationService _notifications;
    private readonly ILogger<GuildService> _logger;

    private static readonly Dictionary<GuildRole, HashSet<GuildPermission>> PermissionMatrix = new()
    {
        [GuildRole.Leader] = new(Enum.GetValues<GuildPermission>()),
        [GuildRole.Officer] = new()
        {
            GuildPermission.ViewInfo,
            GuildPermission.ViewMembers,
            GuildPermission.InviteMembers,
            GuildPermission.PublishStrategy,
            GuildPermission.PostChat,
            GuildPermission.AttackBoss
        },
        [GuildRole.Member] = new()
        {
            GuildPermission.ViewInfo,
            GuildPermission.ViewMembers,
            GuildPermission.PostChat,
            GuildPermission.AttackBoss
        }
    };

    public GuildService(GameDbContext context, INotificationService notifications, ILogger<GuildService> logger)
    {
        _context = context;
        _notifications = notifications;
        _logger = logger;
    }

    public async Task<Guild> CreateGuildAsync(Guid playerId, string name, string tag, string description)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 50)
            throw new InvalidOperationException("Guild name must be 1-50 characters.");
        if (string.IsNullOrWhiteSpace(tag) || tag.Length > 5)
            throw new InvalidOperationException("Guild tag must be 1-5 characters.");
        if (description != null && description.Length > 500)
            throw new InvalidOperationException("Description must be under 500 characters.");

        var player = await _context.Players.FindAsync(playerId)
            ?? throw new KeyNotFoundException("Player not found.");

        if (player.CurrentTier < SubscriptionTier.Premium)
            throw new InvalidOperationException("Creating a guild requires Premium tier or higher.");

        var existingMembership = await _context.GuildMemberships
            .AnyAsync(m => m.PlayerId == playerId);
        if (existingMembership)
            throw new InvalidOperationException("You are already in a guild. Leave your current guild first.");

        if (await _context.Guilds.AnyAsync(g => g.Name == name))
            throw new InvalidOperationException("A guild with that name already exists.");

        if (await _context.Guilds.AnyAsync(g => g.Tag == tag.ToUpperInvariant()))
            throw new InvalidOperationException("A guild with that tag already exists.");

        var guild = new Guild
        {
            Id = Guid.NewGuid(),
            Name = name,
            Tag = tag.ToUpperInvariant(),
            Description = description ?? string.Empty,
            LeaderId = playerId,
            Level = 1,
            MaxMembers = 20,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var membership = new GuildMembership
        {
            Id = Guid.NewGuid(),
            GuildId = guild.Id,
            PlayerId = playerId,
            Role = GuildRole.Leader,
            JoinedAt = DateTime.UtcNow
        };

        _context.Guilds.Add(guild);
        _context.GuildMemberships.Add(membership);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Player {PlayerId} created guild {GuildName} [{GuildTag}]", playerId, name, tag);
        return guild;
    }

    public async Task DeleteGuildAsync(Guid guildId, Guid playerId)
    {
        var guild = await _context.Guilds.FindAsync(guildId)
            ?? throw new KeyNotFoundException("Guild not found.");

        if (guild.LeaderId != playerId)
            throw new InvalidOperationException("Only the guild leader can delete the guild.");

        _context.Guilds.Remove(guild);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Guild {GuildName} deleted by leader {PlayerId}", guild.Name, playerId);
    }

    public async Task<Guild?> GetGuildAsync(Guid guildId)
    {
        return await _context.Guilds
            .Include(g => g.Leader)
            .Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.Id == guildId);
    }

    public async Task<Guild?> GetPlayerGuildAsync(Guid playerId)
    {
        var membership = await _context.GuildMemberships
            .Include(m => m.Guild)
                .ThenInclude(g => g.Leader)
            .Include(m => m.Guild)
                .ThenInclude(g => g.Members)
            .FirstOrDefaultAsync(m => m.PlayerId == playerId);

        return membership?.Guild;
    }

    public async Task<List<GuildMembership>> GetMembersAsync(Guid guildId)
    {
        return await _context.GuildMemberships
            .Include(m => m.Player)
            .Where(m => m.GuildId == guildId)
            .OrderByDescending(m => m.Role)
            .ThenBy(m => m.JoinedAt)
            .ToListAsync();
    }

    public async Task<GuildInvite> InvitePlayerAsync(Guid guildId, Guid invitedByPlayerId, string targetUsername)
    {
        var membership = await _context.GuildMemberships
            .FirstOrDefaultAsync(m => m.GuildId == guildId && m.PlayerId == invitedByPlayerId)
            ?? throw new InvalidOperationException("You are not a member of this guild.");

        if (!HasPermission(membership.Role, GuildPermission.InviteMembers))
            throw new InvalidOperationException("You do not have permission to invite members.");

        var targetPlayer = await _context.Players
            .FirstOrDefaultAsync(p => p.Username == targetUsername)
            ?? throw new KeyNotFoundException($"Player '{targetUsername}' not found.");

        var existingMembership = await _context.GuildMemberships
            .AnyAsync(m => m.PlayerId == targetPlayer.Id);
        if (existingMembership)
            throw new InvalidOperationException($"{targetUsername} is already in a guild.");

        var pendingInvite = await _context.GuildInvites
            .AnyAsync(i => i.GuildId == guildId
                && i.InvitedPlayerId == targetPlayer.Id
                && i.Status == GuildInviteStatus.Pending
                && i.ExpiresAt > DateTime.UtcNow);
        if (pendingInvite)
            throw new InvalidOperationException($"{targetUsername} already has a pending invite to this guild.");

        var guild = await _context.Guilds.FirstAsync(g => g.Id == guildId);
        var memberCount = await _context.GuildMemberships.CountAsync(m => m.GuildId == guildId);
        if (memberCount >= guild.MaxMembers)
            throw new InvalidOperationException("Guild is full.");

        var invite = new GuildInvite
        {
            Id = Guid.NewGuid(),
            GuildId = guildId,
            InvitedPlayerId = targetPlayer.Id,
            InvitedByPlayerId = invitedByPlayerId,
            Status = GuildInviteStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        _context.GuildInvites.Add(invite);
        await _context.SaveChangesAsync();

        var guildName = guild.Name;
        await _notifications.SendAsync(targetPlayer.Id, Models.Enums.NotificationType.GuildInvited, "Guild Invitation",
            $"You've been invited to join [{guildName}]!", "/api/v1/guild/invites");

        return invite;
    }

    public async Task<GuildInvite> AcceptInviteAsync(Guid inviteId, Guid playerId)
    {
        var invite = await _context.GuildInvites
            .Include(i => i.Guild)
                .ThenInclude(g => g.Members)
            .FirstOrDefaultAsync(i => i.Id == inviteId && i.InvitedPlayerId == playerId)
            ?? throw new KeyNotFoundException("Invite not found.");

        if (invite.Status != GuildInviteStatus.Pending)
            throw new InvalidOperationException($"Invite is no longer pending (status: {invite.Status}).");

        if (invite.ExpiresAt <= DateTime.UtcNow)
        {
            invite.Status = GuildInviteStatus.Expired;
            await _context.SaveChangesAsync();
            throw new InvalidOperationException("Invite has expired.");
        }

        var existingMembership = await _context.GuildMemberships
            .AnyAsync(m => m.PlayerId == playerId);
        if (existingMembership)
            throw new InvalidOperationException("You are already in a guild. Leave your current guild first.");

        // Re-check with DB-level count to prevent over-capacity race condition
        var currentMembers = await _context.GuildMemberships.CountAsync(m => m.GuildId == invite.GuildId);
        if (currentMembers >= invite.Guild.MaxMembers)
            throw new InvalidOperationException("Guild is full.");

        invite.Status = GuildInviteStatus.Accepted;

        var membership = new GuildMembership
        {
            Id = Guid.NewGuid(),
            GuildId = invite.GuildId,
            PlayerId = playerId,
            Role = GuildRole.Member,
            JoinedAt = DateTime.UtcNow
        };

        _context.GuildMemberships.Add(membership);

        // Decline all other pending invites for this player
        var otherInvites = await _context.GuildInvites
            .Where(i => i.InvitedPlayerId == playerId
                && i.Status == GuildInviteStatus.Pending
                && i.Id != inviteId)
            .ToListAsync();
        foreach (var other in otherInvites)
            other.Status = GuildInviteStatus.Declined;

        await _context.SaveChangesAsync();

        await _notifications.SendToGuildAsync(invite.GuildId, Models.Enums.NotificationType.GuildInviteResponse, "New Member",
            $"{(await _context.Players.FindAsync(playerId))?.Username} has joined the guild!", excludePlayerId: playerId);

        _logger.LogInformation("Player {PlayerId} joined guild {GuildId}", playerId, invite.GuildId);
        return invite;
    }

    public async Task<GuildInvite> DeclineInviteAsync(Guid inviteId, Guid playerId)
    {
        var invite = await _context.GuildInvites
            .FirstOrDefaultAsync(i => i.Id == inviteId && i.InvitedPlayerId == playerId)
            ?? throw new KeyNotFoundException("Invite not found.");

        if (invite.Status != GuildInviteStatus.Pending)
            throw new InvalidOperationException($"Invite is no longer pending (status: {invite.Status}).");

        invite.Status = GuildInviteStatus.Declined;
        await _context.SaveChangesAsync();

        return invite;
    }

    public async Task<List<GuildInvite>> GetPendingInvitesAsync(Guid playerId)
    {
        return await _context.GuildInvites
            .Include(i => i.Guild)
            .Include(i => i.InvitedBy)
            .Where(i => i.InvitedPlayerId == playerId
                && i.Status == GuildInviteStatus.Pending
                && i.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();
    }

    public async Task KickMemberAsync(Guid guildId, Guid requestingPlayerId, Guid targetPlayerId)
    {
        var requestingMembership = await _context.GuildMemberships
            .FirstOrDefaultAsync(m => m.GuildId == guildId && m.PlayerId == requestingPlayerId)
            ?? throw new InvalidOperationException("You are not a member of this guild.");

        if (!HasPermission(requestingMembership.Role, GuildPermission.KickMembers))
            throw new InvalidOperationException("You do not have permission to kick members.");

        var targetMembership = await _context.GuildMemberships
            .FirstOrDefaultAsync(m => m.GuildId == guildId && m.PlayerId == targetPlayerId)
            ?? throw new KeyNotFoundException("Target player is not a member of this guild.");

        if (targetMembership.Role >= requestingMembership.Role)
            throw new InvalidOperationException("You cannot kick a member with equal or higher rank.");

        _context.GuildMemberships.Remove(targetMembership);
        await _context.SaveChangesAsync();

        await _notifications.SendAsync(targetPlayerId, Models.Enums.NotificationType.GuildKicked, "Removed from Guild",
            "You have been removed from the guild.");

        _logger.LogInformation("Player {TargetId} kicked from guild {GuildId} by {RequesterId}",
            targetPlayerId, guildId, requestingPlayerId);
    }

    public async Task PromoteMemberAsync(Guid guildId, Guid requestingPlayerId, Guid targetPlayerId, GuildRole newRole)
    {
        var guild = await _context.Guilds.FindAsync(guildId)
            ?? throw new KeyNotFoundException("Guild not found.");

        if (guild.LeaderId != requestingPlayerId)
            throw new InvalidOperationException("Only the guild leader can promote members.");

        var targetMembership = await _context.GuildMemberships
            .FirstOrDefaultAsync(m => m.GuildId == guildId && m.PlayerId == targetPlayerId)
            ?? throw new KeyNotFoundException("Target player is not a member of this guild.");

        if (newRole == GuildRole.Leader)
        {
            // Transfer leadership
            var leaderMembership = await _context.GuildMemberships
                .FirstAsync(m => m.GuildId == guildId && m.PlayerId == requestingPlayerId);

            leaderMembership.Role = GuildRole.Officer;
            targetMembership.Role = GuildRole.Leader;
            guild.LeaderId = targetPlayerId;
        }
        else
        {
            targetMembership.Role = newRole;
        }

        guild.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await _notifications.SendAsync(targetPlayerId, Models.Enums.NotificationType.GuildPromoted, "Promoted!",
            $"You've been promoted to {targetMembership.Role} in your guild!");

        _logger.LogInformation("Player {TargetId} promoted to {Role} in guild {GuildId}",
            targetPlayerId, newRole, guildId);
    }

    public async Task LeaveGuildAsync(Guid playerId)
    {
        var membership = await _context.GuildMemberships
            .Include(m => m.Guild)
            .FirstOrDefaultAsync(m => m.PlayerId == playerId)
            ?? throw new InvalidOperationException("You are not in a guild.");

        if (membership.Role == GuildRole.Leader)
            throw new InvalidOperationException(
                "Guild leaders cannot leave. Transfer leadership first or delete the guild.");

        _context.GuildMemberships.Remove(membership);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Player {PlayerId} left guild {GuildId}", playerId, membership.GuildId);
    }

    public async Task<GuildMembership?> GetMembershipAsync(Guid playerId)
    {
        return await _context.GuildMemberships
            .Include(m => m.Guild)
            .FirstOrDefaultAsync(m => m.PlayerId == playerId);
    }

    public bool HasPermission(GuildRole role, GuildPermission permission)
    {
        return PermissionMatrix.TryGetValue(role, out var permissions)
            && permissions.Contains(permission);
    }
}
