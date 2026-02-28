using System.Text.Json;
using ApiCombatGame.Data;
using ApiCombatGame.Models.Domain;
using ApiCombatGame.Models.Enums;
using ApiCombatGame.Models.ViewModels;
using ApiCombatGame.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ApiCombatGame.Services;

public class AdminPlayerManagementService : IAdminPlayerManagementService
{
    private readonly GameDbContext _context;
    private readonly INotificationService _notifications;
    private readonly IActivityLedger _ledger;
    private readonly IAdminAuditService _audit;
    private readonly ILogger<AdminPlayerManagementService> _logger;

    public AdminPlayerManagementService(GameDbContext context, INotificationService notifications, IActivityLedger ledger, IAdminAuditService audit, ILogger<AdminPlayerManagementService> logger)
    {
        _context = context;
        _notifications = notifications;
        _ledger = ledger;
        _audit = audit;
        _logger = logger;
    }

    public async Task<bool> ToggleAdminAsync(Guid adminPlayerId, Guid playerId, bool isAdmin, AdminRole role)
    {
        var player = await _context.Players.FindAsync(playerId);
        if (player == null) return false;

        var oldState = $"IsAdmin={player.IsAdmin}, Role={player.AdminRole}";
        player.IsAdmin = isAdmin;
        player.AdminRole = isAdmin ? role : AdminRole.None;
        _audit.AddEntry(adminPlayerId, "ToggleAdmin", playerId, $"{{\"old\":\"{oldState}\",\"new\":\"IsAdmin={isAdmin}, Role={role}\"}}");
        await _context.SaveChangesAsync();

        await _notifications.SendAsync(playerId, NotificationType.AdminActionOnAccount, "Admin Status Changed",
            isAdmin ? $"You have been granted admin role: {role}" : "Your admin access has been removed.");

        _logger.LogInformation("Admin status changed for {Username}: IsAdmin={IsAdmin}, Role={Role}", player.Username, isAdmin, role);
        return true;
    }

    public async Task<bool> ToggleEducatorAsync(Guid adminPlayerId, Guid playerId, bool isEducator)
    {
        var player = await _context.Players.FindAsync(playerId);
        if (player == null) return false;

        player.IsEducator = isEducator;
        _audit.AddEntry(adminPlayerId, "ToggleEducator", playerId, $"{{\"isEducator\":{isEducator.ToString().ToLower()}}}");
        await _context.SaveChangesAsync();
        _logger.LogInformation("Educator status changed for {Username}: IsEducator={IsEducator}", player.Username, isEducator);
        return true;
    }

    public async Task<bool> AdjustCurrencyAsync(Guid adminPlayerId, Guid playerId, int amount)
    {
        var player = await _context.Players.FindAsync(playerId);
        if (player == null) return false;

        var oldBalance = player.Currency;
        player.Currency += amount;
        if (player.Currency < 0) player.Currency = 0;
        _ledger.LogPlayer(playerId, "Currency", oldBalance, player.Currency, "AdminAction", "AdminAdjust");
        _audit.AddEntry(adminPlayerId, "AdjustCurrency", playerId, $"{{\"amount\":{amount},\"oldBalance\":{oldBalance},\"newBalance\":{player.Currency}}}");
        await _context.SaveChangesAsync();

        await _notifications.SendAsync(playerId, NotificationType.AdminActionOnAccount, "Currency Adjusted",
            $"An administrator adjusted your gold by {(amount >= 0 ? "+" : "")}{amount}. New balance: {player.Currency}g.");

        _logger.LogInformation("Currency adjusted for {Username}: {Amount} (new balance: {Balance})", player.Username, amount, player.Currency);
        return true;
    }

    public async Task<bool> AdjustRatingAsync(Guid adminPlayerId, Guid playerId, int amount)
    {
        var player = await _context.Players.FindAsync(playerId);
        if (player == null) return false;

        var oldRating = player.Rating;
        player.Rating = Math.Max(100, player.Rating + amount);
        _ledger.LogPlayer(playerId, "Rating", oldRating, player.Rating, "AdminAction", "AdminAdjust");
        _audit.AddEntry(adminPlayerId, "AdjustRating", playerId, $"{{\"amount\":{amount},\"oldRating\":{oldRating},\"newRating\":{player.Rating}}}");
        await _context.SaveChangesAsync();

        await _notifications.SendAsync(playerId, NotificationType.AdminActionOnAccount, "Rating Adjusted",
            $"An administrator adjusted your rating by {(amount >= 0 ? "+" : "")}{amount}. New rating: {player.Rating}.");

        _logger.LogInformation("Rating adjusted for {Username}: {Amount} (new rating: {Rating})", player.Username, amount, player.Rating);
        return true;
    }

    public async Task<bool> SetTierAsync(Guid adminPlayerId, Guid playerId, SubscriptionTier tier)
    {
        var player = await _context.Players.FindAsync(playerId);
        if (player == null) return false;

        var oldTier = player.CurrentTier;
        player.CurrentTier = tier;
        _audit.AddEntry(adminPlayerId, "SetTier", playerId, $"{{\"oldTier\":\"{oldTier}\",\"newTier\":\"{tier}\"}}");
        _context.SubscriptionEvents.Add(new SubscriptionEvent
        {
            Id = Guid.NewGuid(),
            PlayerId = playerId,
            EventType = oldTier < tier ? "upgraded" : "downgraded",
            OldTier = oldTier,
            NewTier = tier,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        await _notifications.SendAsync(playerId, NotificationType.TierChanged, "Subscription Tier Changed",
            $"Your subscription tier has been changed from {oldTier} to {tier} by an administrator.");

        _logger.LogInformation("Tier changed for {Username}: {Tier}", player.Username, tier);
        return true;
    }

    public async Task<bool> ResetPasswordAsync(Guid adminPlayerId, Guid playerId, string newPassword)
    {
        var player = await _context.Players.FindAsync(playerId);
        if (player == null) return false;

        player.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        _audit.AddEntry(adminPlayerId, "ResetPassword", playerId);
        await _context.SaveChangesAsync();

        await _notifications.SendAsync(playerId, NotificationType.PasswordChanged, "Password Reset",
            "Your password was reset by an administrator. If you did not request this, please contact support.");

        _logger.LogInformation("Password reset for {Username}", player.Username);
        return true;
    }

    public async Task<List<AdminAlert>> GetActiveAlertsAsync()
    {
        return await _context.AdminAlerts
            .AsNoTracking()
            .Where(a => !a.IsAcknowledged)
            .OrderByDescending(a => a.Severity)
            .ThenByDescending(a => a.CreatedAt)
            .Take(25)
            .ToListAsync();
    }

    public async Task AcknowledgeAlertAsync(Guid adminPlayerId, Guid alertId)
    {
        var alert = await _context.AdminAlerts.FindAsync(alertId);
        if (alert == null) return;

        alert.IsAcknowledged = true;
        var json = JsonSerializer.Serialize(new { alertId, category = alert.Category, message = alert.Message });
        _audit.AddEntry(adminPlayerId, "AcknowledgeAlert", null, json);
        await _context.SaveChangesAsync();
    }

    public async Task<AdminAuditLogData> GetAuditLogsAsync(string? actionFilter, int page, int pageSize = 25)
    {
        var query = _context.AdminAuditLogs.AsQueryable();
        if (!string.IsNullOrEmpty(actionFilter))
            query = query.Where(l => l.Action == actionFilter);

        var totalCount = await query.CountAsync();

        var entries = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new AdminAuditLogEntry
            {
                Id = l.Id,
                AdminUsername = _context.Players.Where(p => p.Id == l.AdminPlayerId).Select(p => p.Username).FirstOrDefault() ?? "Unknown",
                Action = l.Action,
                TargetUsername = l.TargetPlayerId.HasValue
                    ? _context.Players.Where(p => p.Id == l.TargetPlayerId).Select(p => p.Username).FirstOrDefault()
                    : null,
                TargetPlayerId = l.TargetPlayerId,
                DetailsJson = l.DetailsJson,
                CreatedAt = l.CreatedAt
            })
            .ToListAsync();

        return new AdminAuditLogData
        {
            Entries = entries,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
        };
    }
}
