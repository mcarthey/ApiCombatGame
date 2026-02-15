namespace ApiCombatGame.Models.DTOs.EasterEgg;

public class EggCheckResponse
{
    public bool HasEgg { get; set; }
    public string? Selector { get; set; }
    public double? OffsetX { get; set; }
    public double? OffsetY { get; set; }
    public string? Icon { get; set; }
    public string? Code { get; set; }
    public bool? AlreadyClaimed { get; set; }
}

public class EggRedeemRequest
{
    public string Code { get; set; } = string.Empty;
}

public class EggRedeemResponse
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string? LootType { get; set; }
    public string? LootDescription { get; set; }
    public int LootAmount { get; set; }
    public string? Code { get; set; }
}

public class EggHuntStatsResponse
{
    public int TotalEggsThisWeek { get; set; }
    public int EggsFoundThisWeek { get; set; }
    public int TotalLootEarned { get; set; }
    public int AllTimeEggsFound { get; set; }
}
