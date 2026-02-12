namespace ApiCombatGame.Models.DTOs.Guild;

public class GuildInviteResponse
{
    public Guid InviteId { get; set; }
    public Guid GuildId { get; set; }
    public string GuildName { get; set; } = string.Empty;
    public string GuildTag { get; set; } = string.Empty;
    public string InvitedByUsername { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}
