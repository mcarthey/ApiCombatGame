using System.ComponentModel.DataAnnotations;

namespace ApiCombatGame.Models.DTOs.Marketplace;

public class StrategyUploadRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public string StrategyJson { get; set; } = "{}";

    /// <summary>
    /// Price in currency. 0 = free.
    /// </summary>
    public int Price { get; set; } = 0;
}
