using System.ComponentModel.DataAnnotations;

namespace ApiCombatGame.Models.Domain;

public class Referral
{
    [Key]
    public Guid Id { get; set; }

    public Guid ReferrerId { get; set; }
    public Guid? ReferredPlayerId { get; set; }

    /// <summary>Unique 8-character referral code.</summary>
    [Required]
    [MaxLength(20)]
    public string Code { get; set; } = string.Empty;

    /// <summary>Whether the referrer has claimed their reward for this referral.</summary>
    public bool ReferrerRewardClaimed { get; set; }

    /// <summary>Whether the referred player received their bonus.</summary>
    public bool ReferredRewardClaimed { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RedeemedAt { get; set; }

    // Navigation
    public Player Referrer { get; set; } = null!;
    public Player? ReferredPlayer { get; set; }
}
