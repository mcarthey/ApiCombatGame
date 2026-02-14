using System.ComponentModel.DataAnnotations;

namespace ApiCombatGame.Models.Domain;

public class RivalAssignment
{
    [Key]
    public Guid Id { get; set; }

    public Guid PlayerId { get; set; }
    public Guid RivalId { get; set; }

    public int WinsAgainstRival { get; set; }
    public int LossesAgainstRival { get; set; }

    /// <summary>Bonus gold earned from beating this rival.</summary>
    public int BonusGoldEarned { get; set; }

    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Rivals rotate weekly or when a new season starts.</summary>
    public DateTime ExpiresAt { get; set; }

    // Navigation
    public Player Player { get; set; } = null!;
    public Player Rival { get; set; } = null!;
}
