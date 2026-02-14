using ApiCombatGame.Models.DTOs.Creator;

namespace ApiCombatGame.Services.Interfaces;

public interface IContentCreatorService
{
    Task<CreatorProfileResponse> ApplyAsync(Guid playerId, ApplyCreatorRequest request);
    Task<CreatorProfileResponse?> GetProfileAsync(Guid playerId);
    Task<List<CreatorProfileResponse>> GetVerifiedCreatorsAsync();
    Task<SpotlightResponse> GetSpotlightAsync();
    Task<CreatorStatsResponse> GetCreatorStatsAsync(Guid playerId);
    Task VerifyCreatorAsync(Guid creatorId);
}
