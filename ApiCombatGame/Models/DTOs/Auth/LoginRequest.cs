using System.ComponentModel.DataAnnotations;

namespace ApiCombatGame.Models.DTOs.Auth;

/// <summary>Credentials for authenticating an existing player.</summary>
public class LoginRequest
{
    /// <summary>Your registered email address.</summary>
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    /// <summary>Your account password.</summary>
    [Required]
    public string Password { get; set; } = string.Empty;
}
