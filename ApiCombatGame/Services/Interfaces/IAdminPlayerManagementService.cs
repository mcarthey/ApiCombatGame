using ApiCombatGame.Models.Domain;
using ApiCombatGame.Models.Enums;
using ApiCombatGame.Models.ViewModels;

namespace ApiCombatGame.Services.Interfaces;

public interface IAdminPlayerManagementService
{
    Task<bool> ToggleAdminAsync(Guid adminPlayerId, Guid playerId, bool isAdmin, AdminRole role = AdminRole.SuperAdmin);
    Task<bool> ToggleEducatorAsync(Guid adminPlayerId, Guid playerId, bool isEducator);
    Task<bool> AdjustCurrencyAsync(Guid adminPlayerId, Guid playerId, int amount);
    Task<bool> AdjustRatingAsync(Guid adminPlayerId, Guid playerId, int amount);
    Task<bool> SetTierAsync(Guid adminPlayerId, Guid playerId, SubscriptionTier tier);
    Task<bool> ResetPasswordAsync(Guid adminPlayerId, Guid playerId, string newPassword);

    Task<List<AdminAlert>> GetActiveAlertsAsync();
    Task AcknowledgeAlertAsync(Guid adminPlayerId, Guid alertId);
    Task<AdminAuditLogData> GetAuditLogsAsync(string? actionFilter, int page, int pageSize = 25);
}
