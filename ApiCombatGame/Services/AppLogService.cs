using System.Security.Claims;
using ApiCombatGame.Data;
using ApiCombatGame.Models.Domain;
using ApiCombatGame.Models.Enums;
using ApiCombatGame.Models.ViewModels;
using ApiCombatGame.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ApiCombatGame.Services;

public class AppLogService : IAppLogService
{
    private readonly GameDbContext _context;
    private readonly ILogger<AppLogService> _logger;

    public AppLogService(GameDbContext context, ILogger<AppLogService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<string> LogErrorAsync(Exception ex, HttpContext? context = null, Guid? playerId = null, string? category = null)
    {
        var supportId = GenerateSupportId();

        if (playerId == null && context?.User?.Identity?.IsAuthenticated == true)
        {
            var playerIdClaim = context.User.FindFirst("PlayerId")?.Value;
            if (Guid.TryParse(playerIdClaim, out var pid))
                playerId = pid;
        }

        var log = new AppLog
        {
            Id = Guid.NewGuid(),
            Level = AppLogLevel.Error,
            Category = category ?? "Unhandled",
            Message = ex.Message,
            ExceptionDetails = ex.ToString(),
            StackTrace = ex.StackTrace,
            SupportId = supportId,
            PlayerId = playerId,
            RequestPath = context?.Request.Path.Value,
            HttpMethod = context?.Request.Method,
            IpAddress = context?.Connection.RemoteIpAddress?.ToString(),
            UserAgent = context?.Request.Headers.UserAgent.ToString(),
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            _context.AppLogs.Add(log);
            await _context.SaveChangesAsync();
        }
        catch (Exception dbEx)
        {
            _logger.LogError(dbEx, "Failed to persist app log for support ID {SupportId}", supportId);
        }

        return supportId;
    }

    public async Task LogWarningAsync(string message, string? category = null, Guid? playerId = null)
    {
        await PersistLogAsync(AppLogLevel.Warning, message, category ?? "System", playerId);
    }

    public async Task LogInfoAsync(string message, string? category = null, Guid? playerId = null)
    {
        await PersistLogAsync(AppLogLevel.Info, message, category ?? "System", playerId);
    }

    public async Task<AdminLogData> GetLogsAsync(string? search, string? level, DateTime? from, DateTime? to, int page, int pageSize = 25)
    {
        var query = _context.AppLogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(level) && Enum.TryParse<AppLogLevel>(level, true, out var logLevel))
            query = query.Where(l => l.Level == logLevel);

        if (from.HasValue)
            query = query.Where(l => l.CreatedAt >= from.Value);

        if (to.HasValue)
            query = query.Where(l => l.CreatedAt < to.Value.AddDays(1));

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(l =>
                l.Message.Contains(search) ||
                (l.SupportId != null && l.SupportId.Contains(search)) ||
                (l.ExceptionDetails != null && l.ExceptionDetails.Contains(search)) ||
                l.Category.Contains(search));

        var totalCount = await query.CountAsync();

        var logs = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new AdminLogSummary
            {
                Id = l.Id,
                Level = l.Level.ToString(),
                Category = l.Category,
                Message = l.Message.Length > 120 ? l.Message.Substring(0, 120) + "..." : l.Message,
                SupportId = l.SupportId,
                PlayerUsername = l.Player != null ? l.Player.Username : null,
                PlayerId = l.PlayerId,
                CreatedAt = l.CreatedAt
            })
            .ToListAsync();

        return new AdminLogData
        {
            Logs = logs,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
        };
    }

    public async Task<AppLog?> GetLogByIdAsync(Guid id)
    {
        return await _context.AppLogs
            .Include(l => l.Player)
            .FirstOrDefaultAsync(l => l.Id == id);
    }

    public async Task<AppLog?> GetLogBySupportIdAsync(string supportId)
    {
        return await _context.AppLogs
            .Include(l => l.Player)
            .FirstOrDefaultAsync(l => l.SupportId == supportId);
    }

    private async Task PersistLogAsync(AppLogLevel level, string message, string category, Guid? playerId)
    {
        var log = new AppLog
        {
            Id = Guid.NewGuid(),
            Level = level,
            Category = category,
            Message = message,
            PlayerId = playerId,
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            _context.AppLogs.Add(log);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist {Level} app log: {Message}", level, message);
        }
    }

    private static string GenerateSupportId()
    {
        var bytes = new byte[4];
        Random.Shared.NextBytes(bytes);
        return $"ERR-{bytes[0]:X2}{bytes[1]:X2}-{bytes[2]:X2}{bytes[3]:X2}";
    }
}
