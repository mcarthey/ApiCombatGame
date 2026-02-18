using ApiCombatGame.Models.Domain;
using ApiCombatGame.Models.ViewModels;

namespace ApiCombatGame.Services.Interfaces;

public interface IAppLogService
{
    Task<string> LogErrorAsync(Exception ex, HttpContext? context = null, Guid? playerId = null, string? category = null);
    Task LogWarningAsync(string message, string? category = null, Guid? playerId = null);
    Task LogInfoAsync(string message, string? category = null, Guid? playerId = null);
    Task<AdminLogData> GetLogsAsync(string? search, string? level, DateTime? from, DateTime? to, int page, int pageSize = 25);
    Task<AppLog?> GetLogByIdAsync(Guid id);
    Task<AppLog?> GetLogBySupportIdAsync(string supportId);
}
