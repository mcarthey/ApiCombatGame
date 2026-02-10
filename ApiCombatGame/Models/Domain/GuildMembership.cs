using System.ComponentModel.DataAnnotations;
using ApiCombatGame.Models.Enums;

namespace ApiCombatGame.Models.Domain;

public class GuildMembership
{
    [Key]
    public Guid Id { get; set; }

    public Guid GuildId { get; set; }
    public Guild Guild { get; set; } = null!;

    public Guid PlayerId { get; set; }
    public Player Player { get; set; } = null!;

    public GuildRole Role { get; set; } = GuildRole.Member;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public int ContributionPoints { get; set; } = 0;
}
