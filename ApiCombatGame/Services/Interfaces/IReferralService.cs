using ApiCombatGame.Models.DTOs.Referral;

namespace ApiCombatGame.Services.Interfaces;

public interface IReferralService
{
    Task<ReferralInfoResponse> GetReferralInfoAsync(Guid playerId);
    Task<RedeemReferralResponse> RedeemCodeAsync(Guid newPlayerId, string code);
    Task<ReferralLeaderboardResponse> GetLeaderboardAsync(Guid playerId, int limit);
}
