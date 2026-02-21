using ApiCombatGame.Data;
using ApiCombatGame.Models;
using ApiCombatGame.Models.Domain;
using ApiCombatGame.Models.Enums;
using ApiCombatGame.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace ApiCombatGame.BackgroundJobs;

public class DatabaseBackupJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DatabaseBackupJob> _logger;
    private readonly DatabaseBackupSettings _settings;

    public DatabaseBackupJob(
        IServiceScopeFactory scopeFactory,
        ILogger<DatabaseBackupJob> logger,
        IOptions<DatabaseBackupSettings> settings)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _settings = settings.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.Enabled)
        {
            _logger.LogInformation("Database backup job is disabled");
            return;
        }

        _logger.LogInformation("Database backup job started, scheduled for {Hour}:00 UTC daily", _settings.RunAtUtcHour);

        while (!stoppingToken.IsCancellationRequested)
        {
            // Calculate delay until next run
            var now = DateTime.UtcNow;
            var targetToday = now.Date.AddHours(_settings.RunAtUtcHour);
            var nextRun = now > targetToday ? targetToday.AddDays(1) : targetToday;
            var delay = nextRun - now;

            _logger.LogInformation("Next backup scheduled in {Delay}", delay);
            await Task.Delay(delay, stoppingToken);

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var backupService = scope.ServiceProvider.GetRequiredService<IDatabaseBackupService>();
                var context = scope.ServiceProvider.GetRequiredService<GameDbContext>();

                var result = await backupService.RunBackupAsync(stoppingToken);

                // Create AdminAlert for backup result
                context.AdminAlerts.Add(new AdminAlert
                {
                    Id = Guid.NewGuid(),
                    Severity = result.Success ? AlertSeverity.Info : AlertSeverity.Critical,
                    Category = "DatabaseBackup",
                    Message = result.Success
                        ? $"Backup completed: {result.FileName} ({result.SizeBytes / 1024}KB, {result.TablesExported} tables, {result.TotalRows} rows, {result.Duration.TotalSeconds:F1}s)"
                        : $"Backup FAILED: {result.ErrorMessage}",
                    ActionRequired = result.Success ? null : "Check logs and retry manually.",
                    CreatedAt = DateTime.UtcNow
                });
                await context.SaveChangesAsync(stoppingToken);

                // Cleanup old backups
                await backupService.CleanupOldBackupsAsync(stoppingToken);

                if (result.Success)
                    _logger.LogInformation("Backup completed: {FileName}, {Tables} tables, {Rows} rows", result.FileName, result.TablesExported, result.TotalRows);
                else
                    _logger.LogError("Backup failed: {Error}", result.ErrorMessage);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Database backup job failed");
            }
        }
    }
}
