namespace ApiCombatGame.Models.DTOs.Guild;

public class GuildResponse
{
    public Guid GuildId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Tag { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string LeaderName { get; set; } = string.Empty;
    public int Level { get; set; }
    public int MemberCount { get; set; }
    public int MaxMembers { get; set; }
    public DateTime CreatedAt { get; set; }
}
