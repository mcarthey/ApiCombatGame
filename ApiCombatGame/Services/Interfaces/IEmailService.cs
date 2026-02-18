namespace ApiCombatGame.Services.Interfaces;

public interface IEmailService
{
    Task SendContactEmailAsync(string senderName, string senderEmail, string subject, string message, string? userAgent = null, string? appVersion = null);
    Task SendWelcomeEmailAsync(string email, string username);
    Task SendPasswordResetEmailAsync(string email, string username, string resetLink);
    Task SendAccountDeletionEmailAsync(string email, string username);
    Task SendVerificationEmailAsync(string email, string username, string verifyLink);
}
