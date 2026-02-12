using System.ComponentModel.DataAnnotations;

namespace ApiCombatGame.Models.DTOs.Guild;

public class InvitePlayerRequest
{
    [Required]
    public string Username { get; set; } = string.Empty;
}

public class KickMemberRequest
{
    [Required]
    public Guid PlayerId { get; set; }
}

public class PromoteMemberRequest
{
    [Required]
    public Guid PlayerId { get; set; }

    [Required]
    public string NewRole { get; set; } = string.Empty;
}
