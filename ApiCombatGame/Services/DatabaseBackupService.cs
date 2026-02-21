using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;
using ApiCombatGame.Data;
using ApiCombatGame.Models;
using ApiCombatGame.Services.Interfaces;
using Azure.Storage.Blobs;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ApiCombatGame.Services;

public class DatabaseBackupService : IDatabaseBackupService
{
    private readonly GameDbContext _context;
    private readonly DatabaseBackupSettings _settings;
    private readonly ILogger<DatabaseBackupService> _logger;

    public DatabaseBackupService(
        GameDbContext context,
        IOptions<DatabaseBackupSettings> settings,
        ILogger<DatabaseBackupService> logger)
    {
        _context = context;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<BackupResult> RunBackupAsync(CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var fileName = $"apicombat_backup_{timestamp}.zip";
        int totalRows = 0;
        int tablesExported = 0;

        try
        {
            var connection = _context.Database.GetDbConnection();
            if (connection.State != System.Data.ConnectionState.Open)
                await connection.OpenAsync(ct);

            // Discover tables
            var tables = new List<(string Schema, string Name)>();
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "SELECT TABLE_SCHEMA, TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE' ORDER BY TABLE_NAME";
                using var reader = await cmd.ExecuteReaderAsync(ct);
                while (await reader.ReadAsync(ct))
                    tables.Add((reader.GetString(0), reader.GetString(1)));
            }

            using var memoryStream = new MemoryStream();
            using (var zip = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: true))
            {
                foreach (var (schema, table) in tables)
                {
                    var rows = new List<Dictionary<string, object?>>();
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = $"SELECT * FROM [{schema}].[{table}]";
                        using var reader = await cmd.ExecuteReaderAsync(ct);
                        while (await reader.ReadAsync(ct))
                        {
                            var row = new Dictionary<string, object?>();
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                var value = reader.GetValue(i);
                                row[reader.GetName(i)] = value switch
                                {
                                    DBNull => null,
                                    byte[] bytes => Convert.ToBase64String(bytes),
                                    _ => value
                                };
                            }
                            rows.Add(row);
                        }
                    }

                    var entry = zip.CreateEntry($"{table}.json", CompressionLevel.Optimal);
                    using var entryStream = entry.Open();
                    await JsonSerializer.SerializeAsync(entryStream, rows, new JsonSerializerOptions
                    {
                        WriteIndented = false
                    }, ct);

                    totalRows += rows.Count;
                    tablesExported++;
                }
            }

            memoryStream.Position = 0;
            var zipBytes = memoryStream.ToArray();

            // Upload to Azure Blob Storage if configured
            if (!string.IsNullOrEmpty(_settings.AzureBlobConnectionString))
            {
                var blobClient = new BlobServiceClient(_settings.AzureBlobConnectionString);
                var container = blobClient.GetBlobContainerClient(_settings.AzureBlobContainerName);
                await container.CreateIfNotExistsAsync(cancellationToken: ct);
                var blob = container.GetBlobClient(fileName);
                using var uploadStream = new MemoryStream(zipBytes);
                await blob.UploadAsync(uploadStream, overwrite: true, cancellationToken: ct);
                _logger.LogInformation("Backup uploaded to Azure Blob: {FileName} ({Size} bytes)", fileName, zipBytes.Length);
            }

            sw.Stop();

            return new BackupResult
            {
                Success = true,
                FileName = fileName,
                SizeBytes = zipBytes.Length,
                TablesExported = tablesExported,
                TotalRows = totalRows,
                Duration = sw.Elapsed,
                ZipBytes = zipBytes
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Database backup failed");
            return new BackupResult
            {
                Success = false,
                FileName = fileName,
                Duration = sw.Elapsed,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task CleanupOldBackupsAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_settings.AzureBlobConnectionString))
            return;

        try
        {
            var blobClient = new BlobServiceClient(_settings.AzureBlobConnectionString);
            var container = blobClient.GetBlobContainerClient(_settings.AzureBlobContainerName);

            if (!await container.ExistsAsync(ct))
                return;

            var cutoff = DateTime.UtcNow.AddDays(-_settings.RetentionDays);
            await foreach (var blob in container.GetBlobsAsync(cancellationToken: ct))
            {
                if (blob.Properties.CreatedOn.HasValue && blob.Properties.CreatedOn.Value < cutoff)
                {
                    await container.DeleteBlobAsync(blob.Name, cancellationToken: ct);
                    _logger.LogInformation("Deleted old backup: {BlobName}", blob.Name);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cleanup old backups");
        }
    }
}
