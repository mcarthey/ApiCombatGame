using ApiCombatGame.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ApiCombatGame.Pages.Auth;

public class VerifyEmailModel : PageModel
{
    private readonly IAuthService _authService;

    public VerifyEmailModel(IAuthService authService)
    {
        _authService = authService;
    }

    public bool Verified { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync(string? token)
    {
        if (string.IsNullOrEmpty(token))
        {
            ErrorMessage = "Missing verification token.";
            return;
        }

        Verified = await _authService.VerifyEmailAsync(token);

        if (!Verified)
            ErrorMessage = "Invalid or expired verification link. You can request a new one from your dashboard.";
    }
}
