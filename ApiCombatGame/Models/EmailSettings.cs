namespace ApiCombatGame.Models;

public class EmailSettings
{
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public string SmtpUsername { get; set; } = string.Empty;
    public string SmtpPassword { get; set; } = string.Empty;
    public string FromAddress { get; set; } = "noreply@apicombat.com";
    public string FromName { get; set; } = "API Combat Game";
    public string SupportAddress { get; set; } = "support@apicombat.com";
}
