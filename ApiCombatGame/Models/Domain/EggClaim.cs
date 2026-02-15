using System.ComponentModel.DataAnnotations;

namespace ApiCombatGame.Models.Domain;

public class EggClaim
{
    [Key]
    public Guid Id { get; set; }

    public Guid PlayerId { get; set; }
    public Player Player { get; set; } = null!;

    public Guid EasterEggId { get; set; }
    public EasterEgg EasterEgg { get; set; } = null!;

    /// <summary>What was awarded: Currency, Gems, XpBoost, Unit.</summary>
    [MaxLength(30)]
    public string LootType { get; set; } = string.Empty;

    /// <summary>Human-readable description, e.g. "500 Gold", "Rare Unit: Shadow Knight".</summary>
    [MaxLength(200)]
    public string LootDescription { get; set; } = string.Empty;

    /// <summary>Numeric amount (currency/gems/xp awarded).</summary>
    public int LootAmount { get; set; }

    public DateTime ClaimedAt { get; set; } = DateTime.UtcNow;
}
