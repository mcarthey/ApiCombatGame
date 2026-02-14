namespace ApiCombatGame.Models.DTOs.Cosmetic;

/// <summary>A cosmetic item in the shop.</summary>
public class CosmeticShopItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Rarity { get; set; } = string.Empty;
    public int GemPrice { get; set; }
    public int GoldPrice { get; set; }
    public bool IsLimited { get; set; }
    public string? RequiredTier { get; set; }
    public bool Locked { get; set; }
    public bool Owned { get; set; }
}

/// <summary>Shop listing with player balance.</summary>
public class CosmeticShopResponse
{
    public int PlayerGems { get; set; }
    public int PlayerGold { get; set; }
    public List<CosmeticShopItem> Items { get; set; } = new();
}

/// <summary>Player's owned cosmetics.</summary>
public class PlayerCosmeticResponse
{
    public Guid CosmeticId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Rarity { get; set; } = string.Empty;
    public bool IsEquipped { get; set; }
}

/// <summary>Purchase request.</summary>
public class PurchaseCosmeticRequest
{
    /// <summary>"gems" or "gold"</summary>
    public string PaymentMethod { get; set; } = "gems";
}

/// <summary>Gem balance info.</summary>
public class GemBalanceResponse
{
    public int Gems { get; set; }
    public int Gold { get; set; }
    public int GemsEarnedTotal { get; set; }
    public int GemsSpentTotal { get; set; }
}
