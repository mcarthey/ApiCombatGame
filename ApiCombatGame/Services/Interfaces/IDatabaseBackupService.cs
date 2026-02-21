namespace ApiCombatGame.Services.Interfaces;

public class BackupResult
{
    public bool Success { get; set; }
    public string FileName { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public int TablesExported { get; set; }
    public int TotalRows { get; set; }
    public TimeSpan Duration { get; set; }
    public string? ErrorMessage { get; set; }
    public byte[]? ZipBytes { get; set; }
}

public interface IDatabaseBackupService
{
    Task<BackupResult> RunBackupAsync(CancellationToken ct = default);
    Task CleanupOldBackupsAsync(CancellationToken ct = default);
}
