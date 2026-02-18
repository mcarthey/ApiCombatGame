using System.ComponentModel.DataAnnotations;
using ApiCombatGame.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ApiCombatGame.Pages.Auth;

public class ForgotPasswordModel : PageModel
{
    private readonly IAuthService _authService;

    public ForgotPasswordModel(IAuthService authService)
    {
        _authService = authService;
    }

    [BindProperty]
    public ForgotPasswordInput Input { get; set; } = new();

    public bool Submitted { get; set; }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        await _authService.RequestPasswordResetAsync(Input.Email);

        // Always show success — don't reveal if email exists
        Submitted = true;
        return Page();
    }

    public class ForgotPasswordInput
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}
