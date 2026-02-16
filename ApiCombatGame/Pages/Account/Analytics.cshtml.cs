using ApiCombatGame.Models.ViewModels;
using ApiCombatGame.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace ApiCombatGame.Pages.Account;

[Authorize]
public class AnalyticsModel : PageModel
{
    private readonly IPlayerAnalyticsService _analyticsService;

    public AnalyticsModel(IPlayerAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    public PlayerAnalyticsViewModel Analytics { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var playerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(playerIdClaim) || !Guid.TryParse(playerIdClaim, out var playerId))
        {
            return RedirectToPage("/Auth/Login");
        }

        Analytics = await _analyticsService.GetPlayerAnalyticsAsync(playerId);

        return Page();
    }
}
