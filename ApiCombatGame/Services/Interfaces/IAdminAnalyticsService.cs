using ApiCombatGame.Models.Enums;
using ApiCombatGame.Models.ViewModels;

namespace ApiCombatGame.Services.Interfaces;

public interface IAdminAnalyticsService
{
    // Overview
    Task<AdminOverviewData> GetOverviewAsync();

    // Player analytics
    Task<AdminPlayerAnalyticsData> GetPlayerAnalyticsAsync(string? search = null, string? tierFilter = null, int page = 1, int pageSize = 25, bool hideBots = false, string? sortBy = null, bool sortDesc = true);
    Task<AdminPlayerDetailData?> GetPlayerDetailAsync(Guid playerId);

    // Meta/Balance
    Task<AdminMetaData> GetMetaDataAsync(int days = 7);

    // Guild analytics
    Task<AdminGuildAnalyticsData> GetGuildAnalyticsAsync();

    // Technical
    Task<AdminTechnicalData> GetTechnicalDataAsync();

    // Admin actions (adminPlayerId is the admin performing the action, for audit logging)
    Task<bool> ToggleAdminAsync(Guid adminPlayerId, Guid playerId, bool isAdmin, AdminRole role = AdminRole.SuperAdmin);
    Task<bool> AdjustCurrencyAsync(Guid adminPlayerId, Guid playerId, int amount);
    Task<bool> SetTierAsync(Guid adminPlayerId, Guid playerId, SubscriptionTier tier);
    Task<bool> ResetPasswordAsync(Guid adminPlayerId, Guid playerId, string newPassword);
}
