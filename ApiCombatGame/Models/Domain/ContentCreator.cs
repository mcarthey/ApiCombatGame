using System.ComponentModel.DataAnnotations;

namespace ApiCombatGame.Models.Domain;

/// <summary>
/// Content Creator profile for players who create strategies, tutorials, or educational content.
/// </summary>
public class ContentCreator
{
    [Key]
    public Guid Id { get; set; }

    public Guid PlayerId { get; set; }
    public Player Player { get; set; } = null!;

    /// <summary>Creator display name.</summary>
    [Required]
    [MaxLength(50)]
    public string CreatorName { get; set; } = string.Empty;

    /// <summary>Short bio/description of what they create.</summary>
    [MaxLength(500)]
    public string Bio { get; set; } = string.Empty;

    /// <summary>Whether the creator has been verified by admins.</summary>
    public bool IsVerified { get; set; } = false;

    /// <summary>Total revenue share gems earned from content downloads.</summary>
    public int GemsEarned { get; set; } = 0;

    /// <summary>Total strategy downloads across all their content.</summary>
    public int TotalDownloads { get; set; } = 0;

    /// <summary>Total modules created (educational content).</summary>
    public int ModulesCreated { get; set; } = 0;

    /// <summary>Whether featured in the monthly spotlight.</summary>
    public bool IsSpotlighted { get; set; } = false;

    /// <summary>Month when last spotlighted (yyyy-MM format).</summary>
    [MaxLength(10)]
    public string? SpotlightMonth { get; set; }

    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
    public DateTime? VerifiedAt { get; set; }
}
