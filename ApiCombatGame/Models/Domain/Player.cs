using System.ComponentModel.DataAnnotations;
using ApiCombatGame.Models.Enums;

namespace ApiCombatGame.Models.Domain;

public class Player
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    public int Level { get; set; } = 1;
    public int Currency { get; set; } = 1000;
    public int Rating { get; set; } = 1000;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastLoginAt { get; set; } = DateTime.UtcNow;

    // Subscription info
    public SubscriptionTier CurrentTier { get; set; } = SubscriptionTier.Free;
    public int DailyBattlesUsed { get; set; }
    public DateTime LastBattleResetDate { get; set; } = DateTime.UtcNow.Date;

    // Navigation properties
    public List<Unit> Roster { get; set; } = new();
    public List<Team> Teams { get; set; } = new();
    public Subscription? Subscription { get; set; }
    public List<ApiKey> ApiKeys { get; set; } = new();
}
