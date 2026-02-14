using System.ComponentModel.DataAnnotations;

namespace ApiCombatGame.Models.Domain;

/// <summary>
/// A cosmetic item available in the shop (skins, profile borders, card backs, etc.).
/// </summary>
public class CosmeticItem
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    /// <summary>"unit_skin", "profile_border", "card_back", "battle_effect"</summary>
    [Required]
    [MaxLength(30)]
    public string Category { get; set; } = string.Empty;

    /// <summary>"common", "rare", "epic", "legendary"</summary>
    [MaxLength(20)]
    public string Rarity { get; set; } = "common";

    /// <summary>Price in gems (premium currency). 0 = not purchasable with gems.</summary>
    public int GemPrice { get; set; }

    /// <summary>Price in gold (standard currency). 0 = not purchasable with gold.</summary>
    public int GoldPrice { get; set; }

    /// <summary>Whether this item is available for purchase.</summary>
    public bool IsAvailable { get; set; } = true;

    /// <summary>Whether this is a limited-time item.</summary>
    public bool IsLimited { get; set; } = false;

    /// <summary>Minimum subscription tier required to purchase. Null = available to all.</summary>
    [MaxLength(20)]
    public string? RequiredTier { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// A cosmetic item owned by a player.
/// </summary>
public class PlayerCosmetic
{
    [Key]
    public Guid Id { get; set; }

    public Guid PlayerId { get; set; }
    public Player Player { get; set; } = null!;

    public Guid CosmeticItemId { get; set; }
    public CosmeticItem CosmeticItem { get; set; } = null!;

    /// <summary>Whether this cosmetic is currently equipped/active.</summary>
    public bool IsEquipped { get; set; } = false;

    public DateTime AcquiredAt { get; set; } = DateTime.UtcNow;
}
