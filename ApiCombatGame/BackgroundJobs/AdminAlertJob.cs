using ApiCombatGame.Data;
using ApiCombatGame.Models.Domain;
using ApiCombatGame.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace ApiCombatGame.BackgroundJobs;

public class AdminAlertJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AdminAlertJob> _logger;

    public AdminAlertJob(IServiceScopeFactory scopeFactory, ILogger<AdminAlertJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<GameDbContext>();

                await CheckBattleQueueHealth(context, stoppingToken);
                await CheckPlayerGrowth(context, stoppingToken);
                await CheckExpiredBosses(context, stoppingToken);

                await context.SaveChangesAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error running admin alert checks");
            }

            // Run every hour
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }

    private async Task CheckBattleQueueHealth(GameDbContext context, CancellationToken ct)
    {
        var queuedCount = await context.Battles
            .CountAsync(b => b.Status == BattleStatus.Queued && b.QueuedAt < DateTime.UtcNow.AddMinutes(-30), ct);

        if (queuedCount > 10)
        {
            var existing = await context.AdminAlerts.AnyAsync(a =>
                a.Category == "BattleQueue" && !a.IsAcknowledged && a.CreatedAt > DateTime.UtcNow.AddHours(-6), ct);

            if (!existing)
            {
                context.AdminAlerts.Add(new AdminAlert
                {
                    Id = Guid.NewGuid(),
                    Severity = AlertSeverity.Warning,
                    Category = "BattleQueue",
                    Message = $"{queuedCount} battles stuck in queue for over 30 minutes.",
                    ActionRequired = "Check the BackgroundBattleProcessor service is running.",
                    CreatedAt = DateTime.UtcNow
                });
            }
        }
    }

    private async Task CheckPlayerGrowth(GameDbContext context, CancellationToken ct)
    {
        var today = DateTime.UtcNow.Date;
        var signupsToday = await context.Players.CountAsync(p => p.CreatedAt >= today, ct);

        if (signupsToday >= 100)
        {
            var existing = await context.AdminAlerts.AnyAsync(a =>
                a.Category == "GrowthMilestone" && a.CreatedAt >= today, ct);

            if (!existing)
            {
                context.AdminAlerts.Add(new AdminAlert
                {
                    Id = Guid.NewGuid(),
                    Severity = AlertSeverity.Info,
                    Category = "GrowthMilestone",
                    Message = $"Growth milestone: {signupsToday} new signups today!",
                    CreatedAt = DateTime.UtcNow
                });
            }
        }
    }

    private async Task CheckExpiredBosses(GameDbContext context, CancellationToken ct)
    {
        var expiredCount = await context.GuildBosses
            .CountAsync(b => !b.IsDefeated && b.ExpiresAt <= DateTime.UtcNow && b.ExpiresAt > DateTime.UtcNow.AddHours(-2), ct);

        if (expiredCount > 0)
        {
            var existing = await context.AdminAlerts.AnyAsync(a =>
                a.Category == "BossExpiry" && !a.IsAcknowledged && a.CreatedAt > DateTime.UtcNow.AddHours(-6), ct);

            if (!existing)
            {
                context.AdminAlerts.Add(new AdminAlert
                {
                    Id = Guid.NewGuid(),
                    Severity = AlertSeverity.Info,
                    Category = "BossExpiry",
                    Message = $"{expiredCount} guild boss(es) expired without being defeated.",
                    CreatedAt = DateTime.UtcNow
                });
            }
        }
    }
}
