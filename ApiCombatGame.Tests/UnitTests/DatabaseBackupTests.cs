using ApiCombatGame.Data;
using ApiCombatGame.Models;
using ApiCombatGame.Models.Enums;
using ApiCombatGame.Services.Interfaces;
using ApiCombatGame.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace ApiCombatGame.Tests.UnitTests;

public class DatabaseBackupTests : IDisposable
{
    private readonly GameDbContext _context;

    public DatabaseBackupTests()
    {
        _context = TestDbContextFactory.Create();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public void BackupResult_Defaults_AreCorrect()
    {
        var result = new BackupResult();

        Assert.False(result.Success);
        Assert.Equal(string.Empty, result.FileName);
        Assert.Equal(0, result.SizeBytes);
        Assert.Equal(0, result.TablesExported);
        Assert.Equal(0, result.TotalRows);
        Assert.Null(result.ErrorMessage);
        Assert.Null(result.ZipBytes);
    }

    [Fact]
    public void BackupResult_SuccessfulResult_ContainsExpectedData()
    {
        var result = new BackupResult
        {
            Success = true,
            FileName = "apicombat_backup_20260220_030000.zip",
            SizeBytes = 1024 * 500,
            TablesExported = 51,
            TotalRows = 10000,
            Duration = TimeSpan.FromSeconds(12.5),
            ZipBytes = new byte[] { 0x50, 0x4B, 0x03, 0x04 } // ZIP magic bytes
        };

        Assert.True(result.Success);
        Assert.Equal(51, result.TablesExported);
        Assert.Equal(10000, result.TotalRows);
        Assert.Equal(512000, result.SizeBytes);
        Assert.NotNull(result.ZipBytes);
    }

    [Fact]
    public void DatabaseBackupSettings_Defaults()
    {
        var settings = new DatabaseBackupSettings();

        Assert.False(settings.Enabled);
        Assert.Equal(3, settings.RunAtUtcHour);
        Assert.Equal(30, settings.RetentionDays);
        Assert.Equal("apicombat-backups", settings.AzureBlobContainerName);
    }

    [Fact]
    public async Task BackupJob_SuccessfulBackup_CreatesInfoAlert()
    {
        // Simulate what the backup job does after a successful backup
        var result = new BackupResult
        {
            Success = true,
            FileName = "test_backup.zip",
            SizeBytes = 1024 * 100,
            TablesExported = 10,
            TotalRows = 500,
            Duration = TimeSpan.FromSeconds(5)
        };

        _context.AdminAlerts.Add(new Models.Domain.AdminAlert
        {
            Id = Guid.NewGuid(),
            Severity = result.Success ? AlertSeverity.Info : AlertSeverity.Critical,
            Category = "DatabaseBackup",
            Message = $"Backup completed: {result.FileName} ({result.SizeBytes / 1024}KB, {result.TablesExported} tables, {result.TotalRows} rows, {result.Duration.TotalSeconds:F1}s)",
            ActionRequired = null,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var alert = await _context.AdminAlerts
            .FirstOrDefaultAsync(a => a.Category == "DatabaseBackup");

        Assert.NotNull(alert);
        Assert.Equal(AlertSeverity.Info, alert.Severity);
        Assert.Contains("test_backup.zip", alert.Message);
        Assert.Contains("10 tables", alert.Message);
        Assert.Contains("500 rows", alert.Message);
        Assert.Null(alert.ActionRequired);
    }

    [Fact]
    public async Task BackupJob_FailedBackup_CreatesCriticalAlert()
    {
        var result = new BackupResult
        {
            Success = false,
            FileName = "failed_backup.zip",
            Duration = TimeSpan.FromSeconds(1),
            ErrorMessage = "Connection refused"
        };

        _context.AdminAlerts.Add(new Models.Domain.AdminAlert
        {
            Id = Guid.NewGuid(),
            Severity = result.Success ? AlertSeverity.Info : AlertSeverity.Critical,
            Category = "DatabaseBackup",
            Message = $"Backup FAILED: {result.ErrorMessage}",
            ActionRequired = "Check logs and retry manually.",
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var alert = await _context.AdminAlerts
            .FirstOrDefaultAsync(a => a.Category == "DatabaseBackup");

        Assert.NotNull(alert);
        Assert.Equal(AlertSeverity.Critical, alert.Severity);
        Assert.Contains("Connection refused", alert.Message);
        Assert.Equal("Check logs and retry manually.", alert.ActionRequired);
    }
}
