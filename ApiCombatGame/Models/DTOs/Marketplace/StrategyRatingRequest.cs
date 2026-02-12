using System.ComponentModel.DataAnnotations;

namespace ApiCombatGame.Models.DTOs.Marketplace;

/// <summary>Request to rate a marketplace strategy.</summary>
public class StrategyRatingRequest
{
    /// <summary>Star rating from 1 (poor) to 5 (excellent).</summary>
    [Range(1, 5)]
    public int Rating { get; set; }

    /// <summary>Optional review comment. Up to 500 characters.</summary>
    [MaxLength(500)]
    public string Comment { get; set; } = string.Empty;
}
