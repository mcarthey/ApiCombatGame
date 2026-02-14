using ApiCombatGame.Models.DTOs.Season;

namespace ApiCombatGame.Services.Interfaces;

public interface ISeasonService
{
    Task<CurrentSeasonResponse> GetCurrentSeasonAsync(Guid playerId);
    Task<SeasonLeaderboardResponse> GetSeasonLeaderboardAsync(Guid playerId, int limit, int offset);
    Task<SeasonRewardsResponse> ClaimSeasonRewardsAsync(Guid playerId, Guid seasonId);
    Task UpdateSeasonRatingAsync(Guid playerId, int ratingChange, bool won, bool isDraw);
    Task EnsureActiveSeasonAsync();
}
