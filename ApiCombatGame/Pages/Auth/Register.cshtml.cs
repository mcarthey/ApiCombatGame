using System.ComponentModel.DataAnnotations;
using ApiCombatGame.Models.DTOs.Auth;
using ApiCombatGame.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ApiCombatGame.Pages.Auth;

public class RegisterModel : PageModel
{
    private readonly IAuthService _authService;
    private readonly ILogger<RegisterModel> _logger;

    public RegisterModel(IAuthService authService, ILogger<RegisterModel> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [BindProperty]
    public RegisterInputModel Input { get; set; } = new();

    public string? ErrorMessage { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // Registration temporarily disabled during pre-launch
        ErrorMessage = "Registration is not yet open. Please check back on February 16, 2026.";
        return Page();

        if (!ModelState.IsValid)
            return Page();

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
    }
}
