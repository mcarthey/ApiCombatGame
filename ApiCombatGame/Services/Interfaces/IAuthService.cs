using ApiCombatGame.Models.DTOs.Auth;

namespace ApiCombatGame.Services.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<AuthResponse> RefreshTokenAsync(Guid playerId);
    Task RequestPasswordResetAsync(string email);
    Task<bool> ResetPasswordAsync(string token, string newPassword);
    Task<bool> DeleteAccountAsync(Guid playerId, string password);
    Task SendVerificationEmailAsync(Guid playerId);
    Task<bool> VerifyEmailAsync(string token);
}
