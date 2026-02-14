using System.ComponentModel.DataAnnotations;

namespace ApiCombatGame.Models.DTOs.UnitCustomization;

/// <summary>Request to rename a unit.</summary>
public class RenameUnitRequest
{
    /// <summary>The unit to rename.</summary>
    [Required]
    public Guid UnitId { get; set; }

    /// <summary>New display name for the unit (3-50 characters).</summary>
    [Required]
    [MinLength(3)]
    [MaxLength(50)]
    public string NewName { get; set; } = string.Empty;
}

/// <summary>Request to reroll a unit's stats.</summary>
public class RerollStatsRequest
{
    /// <summary>The unit to reroll.</summary>
    [Required]
    public Guid UnitId { get; set; }
}

/// <summary>Request to apply golden upgrade.</summary>
public class GoldenUpgradeRequest
{
    /// <summary>The unit to upgrade.</summary>
    [Required]
    public Guid UnitId { get; set; }
}

/// <summary>Result of a customization action.</summary>
public class UnitCustomizationResponse
{
    /// <summary>Updated unit details.</summary>
    public Guid UnitId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? CustomName { get; set; }
    public bool IsGolden { get; set; }
    public int RerollCount { get; set; }

    /// <summary>Current stats after any changes.</summary>
    public int Health { get; set; }
    public int Attack { get; set; }
    public int Defense { get; set; }
    public int Speed { get; set; }

    /// <summary>Gold spent on this action.</summary>
    public int GoldSpent { get; set; }

    /// <summary>Player's remaining currency.</summary>
    public int RemainingCurrency { get; set; }
}
