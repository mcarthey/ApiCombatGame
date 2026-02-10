using System.ComponentModel.DataAnnotations;

namespace ApiCombatGame.Models.Domain;

public class PlayerAchievement
{
    [Key]
    public Guid Id { get; set; }

    public Guid PlayerId { get; set; }
    public Player Player { get; set; } = null!;

    public Guid AchievementId { get; set; }
    public Achievement Achievement { get; set; } = null!;

    public int Progress { get; set; } = 0;
    public int RequiredProgress { get; set; }
    public bool IsUnlocked { get; set; } = false;

    public DateTime? UnlockedAt { get; set; }
}
