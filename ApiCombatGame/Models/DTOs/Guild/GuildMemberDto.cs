namespace ApiCombatGame.Models.DTOs.Guild;

public class GuildMemberDto
{
    public Guid PlayerId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; }
    public int ContributionPoints { get; set; }
    public int Rating { get; set; }
    public int Level { get; set; }
}
