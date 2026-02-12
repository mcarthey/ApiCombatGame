using System.ComponentModel.DataAnnotations;

namespace ApiCombatGame.Models.Domain;

public class GuildInvite
{
    [Key]
    public Guid Id { get; set; }

    public Guid GuildId { get; set; }
    public Guild Guild { get; set; } = null!;

    public Guid InvitedPlayerId { get; set; }
    public Player InvitedPlayer { get; set; } = null!;

    public Guid InvitedByPlayerId { get; set; }
    public Player InvitedBy { get; set; } = null!;

    public GuildInviteStatus Status { get; set; } = GuildInviteStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddDays(7);
}

public enum GuildInviteStatus
{
    Pending,
    Accepted,
    Declined,
    Expired
}
