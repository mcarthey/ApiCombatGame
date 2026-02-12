using System.ComponentModel.DataAnnotations;

namespace ApiCombatGame.Models.DTOs.Auth;

/// <summary>Credentials for creating a new player account.</summary>
public class RegisterRequest
{
    /// <summary>Your unique display name, visible to other players. 3-50 characters.</summary>
    [Required]
    [MinLength(3)]
    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    /// <summary>Email address for account recovery. Must be unique across all accounts.</summary>
    [Required]
    [EmailAddress]
    [MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    /// <summary>Account password. Minimum 8 characters.</summary>
    [Required]
    [MinLength(8)]
    [MaxLength(100)]
    public string Password { get; set; } = string.Empty;
}
