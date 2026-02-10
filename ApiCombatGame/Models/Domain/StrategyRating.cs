using System.ComponentModel.DataAnnotations;

namespace ApiCombatGame.Models.Domain;

public class StrategyRating
{
    [Key]
    public Guid Id { get; set; }

    public Guid StrategyId { get; set; }
    public Strategy Strategy { get; set; } = null!;

    public Guid PlayerId { get; set; }
    public Player Player { get; set; } = null!;

    /// <summary>
    /// 1-5 star rating.
    /// </summary>
    public int Rating { get; set; }

    [MaxLength(500)]
    public string Comment { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
