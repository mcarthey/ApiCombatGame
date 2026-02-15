namespace ApiCombatGame.Services.Interfaces;

public interface IEmailTemplateService
{
    /// <summary>Wraps body content in the branded email layout.</summary>
    string Render(string title, string bodyHtml, string? preheader = null);

    /// <summary>Builds the HTML body for the support-facing contact notification.</summary>
    string ContactNotification(string senderName, string senderEmail, string subject, string message);

    /// <summary>Builds the HTML body for the thank-you reply sent to the contact form submitter.</summary>
    string ContactThankYou(string senderName, string subject);
}
