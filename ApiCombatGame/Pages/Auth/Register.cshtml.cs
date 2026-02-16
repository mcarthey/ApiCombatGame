using System.ComponentModel.DataAnnotations;
using ApiCombatGame.Models;
using ApiCombatGame.Models.DTOs.Auth;
using ApiCombatGame.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace ApiCombatGame.Pages.Auth;

public class RegisterModel : PageModel
{
    private readonly IAuthService _authService;
    private readonly IRecaptchaService _recaptchaService;
    private readonly RecaptchaSettings _recaptchaSettings;
    private readonly ILogger<RegisterModel> _logger;

    public RegisterModel(
        IAuthService authService,
        IRecaptchaService recaptchaService,
        IOptions<RecaptchaSettings> recaptchaSettings,
        ILogger<RegisterModel> logger)
    {
        _authService = authService;
        _recaptchaService = recaptchaService;
        _recaptchaSettings = recaptchaSettings.Value;
        _logger = logger;
    }

    [BindProperty]
    public RegisterInputModel Input { get; set; } = new();

    public string? ErrorMessage { get; set; }
    public string RecaptchaSiteKey => _recaptchaSettings.SiteKey;

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // Registration temporarily disabled during pre-launch
        ErrorMessage = "Registration is not yet open. Please check back on February 16, 2026.";
        return Page();

#pragma warning disable CS0162 // Unreachable code - remove this pragma when registration opens Feb 16
        if (!ModelState.IsValid)
            return Page();

        // Validate reCAPTCHA
        var recaptchaResult = await _recaptchaService.ValidateAsync(Input.RecaptchaToken);
        if (!recaptchaResult.Success)
        {
            _logger.LogWarning("reCAPTCHA validation failed during registration: score {Score} for {Email}",
                recaptchaResult.Score, Input.Email);
            ErrorMessage = recaptchaResult.ErrorMessage ?? "Please try again.";
            return Page();
        }

        if (Input.Password != Input.ConfirmPassword)
        {
            ErrorMessage = "Passwords do not match.";
            return Page();
        }

        try
        {
            var request = new RegisterRequest
            {
                Username = Input.Username,
                Email = Input.Email,
                Password = Input.Password
            };

            await _authService.RegisterAsync(request);

            _logger.LogInformation("New player registered via web UI: {Username}", Input.Username);

            return RedirectToPage("/Auth/Login", new { message = "registered" });
        }
        catch (InvalidOperationException ex)
        {
            ErrorMessage = ex.Message;
            return Page();
        }
#pragma warning restore CS0162
    }

    public class RegisterInputModel
    {
        [Required]
        [MinLength(3)]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(8)]
        [MaxLength(100)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        /// <summary>reCAPTCHA v3 token populated by JavaScript.</summary>
        public string RecaptchaToken { get; set; } = string.Empty;
    }
}
