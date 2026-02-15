namespace ApiCombatGame.Services.Interfaces;

public interface IEmailService
{
    Task SendContactEmailAsync(string senderName, string senderEmail, string subject, string message);
}
