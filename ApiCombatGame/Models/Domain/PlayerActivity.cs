using System.ComponentModel.DataAnnotations;

namespace ApiCombatGame.Models.Domain;

public class PlayerActivity
{
    [Key]
    public Guid Id { get; set; }

    public Guid PlayerId { get; set; }
    public Player Player { get; set; } = null!;

    /// <summary>Date-only field for grouping (no time component).</summary>
    public DateTime ActivityDate { get; set; }

    public int RequestCount { get; set; } = 1;
    public DateTime LastRequestAt { get; set; } = DateTime.UtcNow;
}
