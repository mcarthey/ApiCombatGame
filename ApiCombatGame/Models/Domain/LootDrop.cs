using System.ComponentModel.DataAnnotations;
using ApiCombatGame.Models.Enums;

namespace ApiCombatGame.Models.Domain;

public class LootDrop
{
    [Key]
    public Guid Id { get; set; }

    public Guid PlayerId { get; set; }
    public Guid BattleId { get; set; }

    public LootDropType DropType { get; set; }

    /// <summary>Human-readable description (e.g. "Currency Pack: 200g").</summary>
    [MaxLength(200)]
    public string Description { get; set; } = string.Empty;

    /// <summary>Value of the drop (gold amount, XP amount, etc).</summary>
    public int Value { get; set; }

    /// <summary>Whether the player has acknowledged/claimed this drop.</summary>
    public bool Claimed { get; set; }

    public DateTime DroppedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Player Player { get; set; } = null!;
}
