using ApiCombatGame.Models.ViewModels;

namespace ApiCombatGame.Services.Interfaces;

public interface IAdminReconciliationService
{
    Task<ReconciliationPreview> PreviewReconciliationAsync(DateTime? since = null);
    Task<ReconciliationPreview> PreviewPlayerReconciliationAsync(Guid playerId, DateTime? since = null);
    Task<ReconciliationPreview> ExecuteReconciliationAsync(Guid adminPlayerId, DateTime? since = null);
}
