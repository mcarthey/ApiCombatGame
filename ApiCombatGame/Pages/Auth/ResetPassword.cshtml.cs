using System.ComponentModel.DataAnnotations;
using ApiCombatGame.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ApiCombatGame.Pages.Auth;

public class ResetPasswordModel : PageModel
{
    private readonly IAuthService _authService;

    public ResetPasswordModel(IAuthService authService)
    {
        _authService = authService;
    }

    [BindProperty]
    public ResetPasswordInput Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? Token { get; set; }

    public string? ErrorMessage { get; set; }

    public void OnGet()
    {
        if (string.IsNullOrEmpty(Token))
            ErrorMessage = "Invalid or missing reset token.";
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrEmpty(Token))
        {
            ErrorMessage = "Invalid or missing reset token.";
            return Page();
        }

        if (!ModelState.IsValid)
            return Page();

        if (Input.Password != Input.ConfirmPassword)
        {
            ErrorMessage = "Passwords do not match.";
            return Page();
        }

        var success = await _authService.ResetPasswordAsync(Token, Input.Password);

        if (!success)
        {
            ErrorMessage = "This reset link has expired or is invalid. Please request a new one.";
            return Page();
        }

        return RedirectToPage("/Auth/Login", new { message = "password_reset" });
    }

    public class ResetPasswordInput
    {
        [Required]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
