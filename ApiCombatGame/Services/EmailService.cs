using ApiCombatGame.Models;
using ApiCombatGame.Services.Interfaces;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;

namespace ApiCombatGame.Services;

public class EmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly IEmailTemplateService _templates;
    private readonly ILogger<EmailService> _logger;

    public EmailService(
        IOptions<EmailSettings> settings,
        IEmailTemplateService templates,
        ILogger<EmailService> logger)
    {
        _settings = settings.Value;
        _templates = templates;
        _logger = logger;
    }

    public async Task SendContactEmailAsync(
        string senderName, string senderEmail, string subject, string message)
    {
        if (string.IsNullOrWhiteSpace(_settings.SmtpHost))
        {
            _logger.LogWarning("SMTP not configured — contact email from {Email} logged but not sent: {Subject}",
                senderEmail, subject);
            return;
        }

        // 1) Branded notification to support
        var notificationHtml = _templates.ContactNotification(senderName, senderEmail, subject, message);

        var toSupport = new MimeMessage();
        toSupport.From.Add(new MailboxAddress(_settings.FromName, _settings.FromAddress));
        toSupport.To.Add(new MailboxAddress("Support", _settings.SupportAddress));
        toSupport.ReplyTo.Add(new MailboxAddress(senderName, senderEmail));
        toSupport.Subject = $"[Contact Form] {subject}";
        toSupport.Body = new TextPart("html") { Text = notificationHtml };

        // 2) Thank-you reply to the sender
        var thankYouHtml = _templates.ContactThankYou(senderName, subject);

        var toSender = new MimeMessage();
        toSender.From.Add(new MailboxAddress(_settings.FromName, _settings.FromAddress));
        toSender.To.Add(new MailboxAddress(senderName, senderEmail));
        toSender.Subject = "We got your message!";
        toSender.Body = new TextPart("html") { Text = thankYouHtml };

        using var client = new SmtpClient();
        await client.ConnectAsync(_settings.SmtpHost, _settings.SmtpPort,
            MailKit.Security.SecureSocketOptions.StartTls);

        if (!string.IsNullOrEmpty(_settings.SmtpUsername))
            await client.AuthenticateAsync(_settings.SmtpUsername, _settings.SmtpPassword);

        await client.SendAsync(toSupport);
        await client.SendAsync(toSender);
        await client.DisconnectAsync(true);

        _logger.LogInformation("Contact emails sent (notification + thank-you) for {Email} about {Subject}",
            senderEmail, subject);
    }
}
