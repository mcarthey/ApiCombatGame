using ApiCombatGame.Models.ViewModels;

namespace ApiCombatGame.Services.Interfaces;

public interface IPlayerAnalyticsService
{
    Task<PlayerAnalyticsViewModel> GetPlayerAnalyticsAsync(Guid playerId);
}
