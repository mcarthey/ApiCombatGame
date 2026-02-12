using ApiCombatGame.Models.Domain;
using ApiCombatGame.Models.Enums;

namespace ApiCombatGame.Services.Interfaces;

public interface IGuildService
{
    Task<Guild> CreateGuildAsync(Guid playerId, string name, string tag, string description);
    Task DeleteGuildAsync(Guid guildId, Guid playerId);
    Task<Guild?> GetGuildAsync(Guid guildId);
    Task<Guild?> GetPlayerGuildAsync(Guid playerId);
    Task<List<GuildMembership>> GetMembersAsync(Guid guildId);
    Task<GuildInvite> InvitePlayerAsync(Guid guildId, Guid invitedByPlayerId, string targetUsername);
    Task<GuildInvite> AcceptInviteAsync(Guid inviteId, Guid playerId);
    Task<GuildInvite> DeclineInviteAsync(Guid inviteId, Guid playerId);
    Task<List<GuildInvite>> GetPendingInvitesAsync(Guid playerId);
    Task KickMemberAsync(Guid guildId, Guid requestingPlayerId, Guid targetPlayerId);
    Task PromoteMemberAsync(Guid guildId, Guid requestingPlayerId, Guid targetPlayerId, GuildRole newRole);
    Task LeaveGuildAsync(Guid playerId);
    Task<GuildMembership?> GetMembershipAsync(Guid playerId);
    bool HasPermission(GuildRole role, GuildPermission permission);
}

public enum GuildPermission
{
    ViewInfo,
    ViewMembers,
    InviteMembers,
    KickMembers,
    PromoteMembers,
    SpendTreasury,
    DeleteGuild,
    PublishStrategy,
    DeleteAnyStrategy,
    PostChat,
    AttackBoss
}
