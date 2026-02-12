using System.ComponentModel.DataAnnotations;

namespace ApiCombatGame.Models.Domain;

public class Guild
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 3-5 character tag, e.g., "APEX".
    /// </summary>
    [Required]
    [MaxLength(5)]
    public string Tag { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    public Guid LeaderId { get; set; }
    public Player Leader { get; set; } = null!;

    public int Level { get; set; } = 1;
    public int ExperiencePoints { get; set; } = 0;
    public int MaxMembers { get; set; } = 20;

    // Treasury & Upgrades
    public int TreasuryBalance { get; set; } = 0;
    public int GoldBonusPercent { get; set; } = 0;
    public int MaxRaidAttempts { get; set; } = 3;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public List<GuildMembership> Members { get; set; } = new();
    public List<GuildBoss> Bosses { get; set; } = new();
    public List<GuildChatMessage> ChatMessages { get; set; } = new();
    public List<GuildStrategy> Strategies { get; set; } = new();
}
