namespace ApiCombatGame.Models.DTOs.Auth;

public class AuthResponse
{
    public Guid PlayerId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}
