namespace ApiCombatGame.Models.DTOs.Loot;

/// <summary>A loot drop earned from a battle.</summary>
public class LootDropResponse
{
    /// <summary>Unique drop ID.</summary>
    public Guid Id { get; set; }

    /// <summary>Drop type: "CurrencyPack", "XpBoost", "RareTitle", or "CriticalGold".</summary>
    public string DropType { get; set; } = string.Empty;

    /// <summary>Human-readable description of what you received.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Numeric value of the reward (gold, XP, etc).</summary>
    public int Value { get; set; }

    /// <summary>The battle that triggered this drop.</summary>
    public Guid BattleId { get; set; }

    /// <summary>When the drop occurred.</summary>
    public DateTime DroppedAt { get; set; }
}

/// <summary>Pending loot drops waiting to be claimed.</summary>
public class PendingLootResponse
{
    /// <summary>Unclaimed loot drops.</summary>
    public List<LootDropResponse> Drops { get; set; } = new();

    /// <summary>Total unclaimed drops.</summary>
    public int TotalUnclaimed { get; set; }
}

/// <summary>Result of claiming loot drops.</summary>
public class ClaimLootResponse
{
    /// <summary>Total gold awarded from claimed drops.</summary>
    public int GoldAwarded { get; set; }

    /// <summary>Total XP awarded from claimed drops.</summary>
    public int XpAwarded { get; set; }

    /// <summary>Titles unlocked (if any).</summary>
    public List<string> TitlesUnlocked { get; set; } = new();

    /// <summary>Number of drops claimed.</summary>
    public int DropsClaimed { get; set; }
}
