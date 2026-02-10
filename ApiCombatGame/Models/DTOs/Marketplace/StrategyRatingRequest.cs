using System.ComponentModel.DataAnnotations;

namespace ApiCombatGame.Models.DTOs.Marketplace;

public class StrategyRatingRequest
{
    /// <summary>
    /// 1-5 star rating.
    /// </summary>
    [Range(1, 5)]
    public int Rating { get; set; }

    [MaxLength(500)]
    public string Comment { get; set; } = string.Empty;
}
