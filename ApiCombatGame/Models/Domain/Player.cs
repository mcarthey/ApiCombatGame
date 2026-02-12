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

    // Progression
    public int ExperiencePoints { get; set; } = 0;
    public int WinStreak { get; set; } = 0;
    public DateTime? LastFirstBattleBonus { get; set; }

    // Daily login streak
    public int LoginStreak { get; set; } = 0;
    public DateTime? LastLoginRewardDate { get; set; }
    public DateTime? LastLoginRewardClaimedAt { get; set; }

    // Admin
    public bool IsAdmin { get; set; } = false;
    public AdminRole AdminRole { get; set; } = AdminRole.None;

    // Notifications
    public string? NotificationPreferencesJson { get; set; }

    // Phase 3: Progression
    public Guid? ActiveTitleId { get; set; }
    public PlayerTitle? ActiveTitle { get; set; }
    public int AchievementPoints { get; set; } = 0;

    // Navigation properties
    public List<Unit> Roster { get; set; } = new();
    public List<Team> Teams { get; set; } = new();
    public Subscription? Subscription { get; set; }
    public List<ApiKey> ApiKeys { get; set; } = new();

    // Phase 3 navigation
    public GuildMembership? GuildMembership { get; set; }
    public List<DailyChallenge> DailyChallenges { get; set; } = new();
    public List<Strategy> CreatedStrategies { get; set; } = new();
    public List<UnitMastery> UnitMasteries { get; set; } = new();
    public List<PlayerAchievement> Achievements { get; set; } = new();
    public List<GuildBossAttempt> BossAttempts { get; set; } = new();
    public List<Notification> Notifications { get; set; } = new();
}
