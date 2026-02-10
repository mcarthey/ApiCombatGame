namespace ApiCombatGame.Models.DTOs.Marketplace;

public class StrategyResponse
{
    public Guid StrategyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CreatorName { get; set; } = string.Empty;
    public int Price { get; set; }
    public int DownloadCount { get; set; }
    public double AverageRating { get; set; }
    public double WinRate { get; set; }
    public double EffectivenessMultiplier { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class StrategyDownloadResponse
{
    public Guid StrategyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string StrategyJson { get; set; } = "{}";
}
