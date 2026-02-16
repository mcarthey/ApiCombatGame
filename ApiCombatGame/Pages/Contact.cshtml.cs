using System.ComponentModel.DataAnnotations;
using System.Reflection;
using ApiCombatGame.Models;
using ApiCombatGame.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace ApiCombatGame.Pages;

public class ContactModel : PageModel
{
    private readonly IRecaptchaService _recaptchaService;
    private readonly IEmailService _emailService;
    private readonly RecaptchaSettings _recaptchaSettings;
    private readonly ILogger<ContactModel> _logger;

    public ContactModel(
        IRecaptchaService recaptchaService,
        IEmailService emailService,
        IOptions<RecaptchaSettings> recaptchaSettings,
        ILogger<ContactModel> logger)
    {
        _recaptchaService = recaptchaService;
        _emailService = emailService;
        _recaptchaSettings = recaptchaSettings.Value;
        _logger = logger;
    }

    [BindProperty]
    public ContactInputModel Input { get; set; } = new();

    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }
    public string RecaptchaSiteKey => _recaptchaSettings.SiteKey;

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // Honeypot check: if the hidden "Website" field has a value, it's a bot
        if (!string.IsNullOrEmpty(Input.Website))
        {
            _logger.LogWarning("Honeypot triggered on contact form from {Email}", Input.Email);
            SuccessMessage = "Your message has been sent. We'll get back to you soon.";
            return Page();
        }

        if (!ModelState.IsValid)
            return Page();

        // Validate reCAPTCHA
        var recaptchaResult = await _recaptchaService.ValidateAsync(Input.RecaptchaToken);
        if (!recaptchaResult.Success)
        {
            _logger.LogWarning("reCAPTCHA validation failed: score {Score} for {Email}",
                recaptchaResult.Score, Input.Email);
            ErrorMessage = recaptchaResult.ErrorMessage ?? "Please try again.";
            return Page();
        }

        try
        {
            // Extract version and user agent for support context
            var version = System.Reflection.Assembly.GetExecutingAssembly()
                .GetCustomAttribute<System.Reflection.AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion ?? "1.0.0";
            var userAgent = Request.Headers.UserAgent.ToString();

            await _emailService.SendContactEmailAsync(
                Input.Name, Input.Email, Input.Subject, Input.Message, userAgent, version);

            SuccessMessage = "Your message has been sent. We'll get back to you soon.";
            Input = new ContactInputModel();
            ModelState.Clear();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send contact form email");
            ErrorMessage = "Something went wrong. Please try again later or email us directly at support@apicombat.com.";
        }

        return Page();
    }

    public class ContactInputModel
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(200)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        [MaxLength(5000)]
        public string Message { get; set; } = string.Empty;

        /// <summary>Honeypot — should remain empty. Bots auto-fill hidden fields.</summary>
        public string? Website { get; set; }

        /// <summary>reCAPTCHA v3 token populated by JavaScript.</summary>
        public string RecaptchaToken { get; set; } = string.Empty;
    }
}
