using ApiCombatGame.Models.ViewModels;

namespace ApiCombatGame.Services.Interfaces;

public interface IAdminAnalyticsService
{
    Task<AdminOverviewData> GetOverviewAsync();
    Task<AdminPlayerAnalyticsData> GetPlayerAnalyticsAsync(string? search = null, string? tierFilter = null, int page = 1, int pageSize = 25, bool hideBots = false, string? sortBy = null, bool sortDesc = true);
    Task<AdminPlayerDetailData?> GetPlayerDetailAsync(Guid playerId);
    Task<AdminMetaData> GetMetaDataAsync(int days = 7);
    Task<AdminGuildAnalyticsData> GetGuildAnalyticsAsync();
    Task<AdminTechnicalData> GetTechnicalDataAsync();
}
