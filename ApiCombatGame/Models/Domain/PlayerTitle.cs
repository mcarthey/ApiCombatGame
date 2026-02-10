using System.ComponentModel.DataAnnotations;

namespace ApiCombatGame.Models.Domain;

public class PlayerTitle
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Display color hex, e.g., "#FFD700" for gold.
    /// </summary>
    [MaxLength(10)]
    public string ColorHex { get; set; } = "#FFFFFF";

    /// <summary>
    /// Serialized requirements for how to earn this title.
    /// </summary>
    public string UnlockRequirementsJson { get; set; } = "{}";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
