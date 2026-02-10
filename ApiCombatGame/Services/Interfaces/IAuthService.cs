using ApiCombatGame.Models.DTOs.Auth;

namespace ApiCombatGame.Services.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<AuthResponse> RefreshTokenAsync(Guid playerId);
}
