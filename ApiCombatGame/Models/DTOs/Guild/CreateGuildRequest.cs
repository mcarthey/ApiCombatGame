using System.ComponentModel.DataAnnotations;

namespace ApiCombatGame.Models.DTOs.Guild;

public class CreateGuildRequest
{
    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MinLength(3)]
    [MaxLength(5)]
    public string Tag { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;
}
