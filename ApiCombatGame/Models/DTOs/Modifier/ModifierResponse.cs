namespace ApiCombatGame.Models.DTOs.Modifier;

/// <summary>An environmental modifier that affects all battles during its active period.</summary>
public class ModifierResponse
{
    /// <summary>Modifier identifier. Null when returning the "Normal" placeholder.</summary>
    public Guid? ModifierId { get; set; }

    /// <summary>Modifier name (e.g., "Healing Surge", "Glass Cannon", "Normal").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Description of the modifier's effect on battle mechanics.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>UTC start of this modifier's active period. Null for the "Normal" placeholder.</summary>
    public DateTime? StartDate { get; set; }

    /// <summary>UTC end of this modifier's active period. Null for the "Normal" placeholder.</summary>
    public DateTime? EndDate { get; set; }
}
