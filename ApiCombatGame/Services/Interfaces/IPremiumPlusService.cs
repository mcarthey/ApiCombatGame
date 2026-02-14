using ApiCombatGame.Models.DTOs.Premium;

namespace ApiCombatGame.Services.Interfaces;

public interface IPremiumPlusService
{
    Task<PremiumPerksResponse> GetPerksAsync(Guid playerId);
    Task<GemStipendClaimResponse> ClaimMonthlyGemsAsync(Guid playerId);
    Task<BadgeResponse> SetBadgeAsync(Guid playerId, string? badge);
    Task<List<string>> GetAvailableBadgesAsync(Guid playerId);
}
