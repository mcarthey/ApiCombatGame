namespace ApiCombatGame.Models;

public class DatabaseBackupSettings
{
    public bool Enabled { get; set; }
    public int RunAtUtcHour { get; set; } = 3;
    public int RetentionDays { get; set; } = 30;
    public string AzureBlobConnectionString { get; set; } = string.Empty;
    public string AzureBlobContainerName { get; set; } = "apicombat-backups";
}
