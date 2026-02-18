namespace ApiCombatGame.Services.Interfaces;

public interface IEmailTemplateService
{
    /// <summary>Wraps body content in the branded email layout.</summary>
    string Render(string title, string bodyHtml, string? preheader = null);

    /// <summary>Builds the HTML body for the support-facing contact notification.</summary>
    string ContactNotification(string senderName, string senderEmail, string subject, string message, string? userAgent = null, string? appVersion = null);

    /// <summary>Builds the HTML body for the thank-you reply sent to the contact form submitter.</summary>
    string ContactThankYou(string senderName, string subject);

    /// <summary>Builds the HTML body for the welcome email sent to new players after registration.</summary>
    string WelcomeEmail(string username);

    /// <summary>Builds the HTML body for the password reset email.</summary>
    string PasswordResetEmail(string username, string resetLink);

    /// <summary>Builds the HTML body for the account deletion confirmation email.</summary>
    string AccountDeletionEmail(string username);

    /// <summary>Builds the HTML body for the email verification email.</summary>
    string VerificationEmail(string username, string verifyLink);
}
