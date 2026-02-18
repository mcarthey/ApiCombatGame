using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using ApiCombatGame.Data;
using ApiCombatGame.Models;
using ApiCombatGame.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ApiCombatGame.Pages.Auth;

public class LoginModel : PageModel
{
    private readonly GameDbContext _context;
    private readonly IRecaptchaService _recaptchaService;
    private readonly RecaptchaSettings _recaptchaSettings;
    private readonly ILogger<LoginModel> _logger;

    public LoginModel(
        GameDbContext context,
        IRecaptchaService recaptchaService,
        IOptions<RecaptchaSettings> recaptchaSettings,
        ILogger<LoginModel> logger)
    {
        _context = context;
        _recaptchaService = recaptchaService;
        _recaptchaSettings = recaptchaSettings.Value;
        _logger = logger;
    }

    [BindProperty]
    public LoginInputModel Input { get; set; } = new();

    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }
    public string RecaptchaSiteKey => _recaptchaSettings.SiteKey;

    public void OnGet(string? message)
    {
        if (message == "registered")
            SuccessMessage = "Account created successfully! Please log in.";
        else if (message == "password_reset")
            SuccessMessage = "Password reset successfully! Please log in with your new password.";
        else if (message == "deleted")
            SuccessMessage = "Your account has been deleted. We're sorry to see you go.";
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        // Validate reCAPTCHA
        var recaptchaResult = await _recaptchaService.ValidateAsync(Input.RecaptchaToken);
        if (!recaptchaResult.Success)
        {
            _logger.LogWarning("reCAPTCHA validation failed during login for {Email}: score {Score}",
                Input.Email, recaptchaResult.Score);
            ErrorMessage = recaptchaResult.ErrorMessage ?? "Please try again.";
            return Page();
        }

        var player = await _context.Players
            .FirstOrDefaultAsync(p => p.Email == Input.Email);

        if (player == null || !BCrypt.Net.BCrypt.Verify(Input.Password, player.PasswordHash))
        {
            ErrorMessage = "Invalid email or password.";
            return Page();
        }

        if (player.IsDeleted)
        {
            ErrorMessage = "Invalid email or password.";
            return Page();
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, player.Id.ToString()),
            new(ClaimTypes.Name, player.Username),
            new(ClaimTypes.Email, player.Email),
            new("PlayerId", player.Id.ToString()),
            new("CurrentTier", player.CurrentTier.ToString())
        };

        if (player.IsAdmin)
        {
            claims.Add(new Claim(ClaimTypes.Role, "Admin"));
            claims.Add(new Claim("AdminRole", player.AdminRole.ToString()));
        }

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = Input.RememberMe,
            ExpiresUtc = Input.RememberMe ? DateTimeOffset.UtcNow.AddDays(30) : DateTimeOffset.UtcNow.AddHours(8)
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);

        player.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Player {Username} logged in via web UI", player.Username);

        return RedirectToPage("/Dashboard");
    }

    public class LoginInputModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }

        /// <summary>reCAPTCHA v3 token populated by JavaScript.</summary>
        public string RecaptchaToken { get; set; } = string.Empty;
    }
}
