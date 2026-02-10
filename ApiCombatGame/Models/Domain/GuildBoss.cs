using System.ComponentModel.DataAnnotations;

namespace ApiCombatGame.Models.Domain;

public class GuildBoss
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Class name of boss implementation. Allows different boss types with unique mechanics.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string BossType { get; set; } = string.Empty;

    /// <summary>
    /// Serialized boss abilities. Each boss can have unique mechanics defined here.
    /// </summary>
    public string AbilitiesJson { get; set; } = "[]";

    public Guid GuildId { get; set; }
    public Guild Guild { get; set; } = null!;

    public int MaxHp { get; set; }
    public int CurrentHp { get; set; }
    public int Attack { get; set; }
    public int Defense { get; set; }

    public DateTime SpawnedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public bool IsDefeated { get; set; } = false;
    public DateTime? DefeatedAt { get; set; }

    // Rewards
    public int RewardCurrency { get; set; }
    public int RewardExperience { get; set; }

    // Navigation
    public List<GuildBossAttempt> Attempts { get; set; } = new();
}
