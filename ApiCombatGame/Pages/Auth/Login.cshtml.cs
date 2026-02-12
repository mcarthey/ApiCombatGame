using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using ApiCombatGame.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ApiCombatGame.Pages.Auth;

public class LoginModel : PageModel
{
    private readonly GameDbContext _context;
    private readonly ILogger<LoginModel> _logger;

    public LoginModel(GameDbContext context, ILogger<LoginModel> logger)
    {
        _context = context;
        _logger = logger;
    }

    [BindProperty]
    public LoginInputModel Input { get; set; } = new();

    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    public void OnGet(string? message)
    {
        if (message == "registered")
            SuccessMessage = "Account created successfully! Please log in.";
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var player = await _context.Players
            .FirstOrDefaultAsync(p => p.Username == Input.Username);

        if (player == null || !BCrypt.Net.BCrypt.Verify(Input.Password, player.PasswordHash))
        {
            ErrorMessage = "Invalid username or password.";
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

        return RedirectToPage("/Account/Index");
    }

    public class LoginInputModel
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }
    }
}
