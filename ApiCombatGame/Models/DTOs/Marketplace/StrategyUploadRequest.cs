using System.ComponentModel.DataAnnotations;

namespace ApiCombatGame.Models.DTOs.Marketplace;

/// <summary>Request to publish a new strategy to the marketplace.</summary>
public class StrategyUploadRequest
{
    /// <summary>Display name for your strategy. 1-100 characters.</summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Describe your strategy's approach and ideal use case. Up to 500 characters.</summary>
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    /// <summary>The strategy configuration as a JSON string matching the StrategyConfig schema.</summary>
    [Required]
    public string StrategyJson { get; set; } = "{}";

    /// <summary>Price in currency. Set to 0 to share for free. Buyers pay this amount to download.</summary>
    public int Price { get; set; } = 0;
}
