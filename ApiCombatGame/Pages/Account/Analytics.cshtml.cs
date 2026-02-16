using ApiCombatGame.Data;
using ApiCombatGame.Models.ViewModels;
using ApiCombatGame.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ApiCombatGame.Pages.Account;

[Authorize]
public class AnalyticsModel : PageModel
{
    private readonly IPlayerAnalyticsService _analyticsService;
    private readonly GameDbContext _context;

    public AnalyticsModel(IPlayerAnalyticsService analyticsService, GameDbContext context)
    {
        _analyticsService = analyticsService;
        _context = context;
    }

    public PlayerAnalyticsViewModel Analytics { get; set; } = new();
    public string CurrentTier { get; set; } = "Free";
    public bool IsPremiumPlus => CurrentTier == "PremiumPlus";

    public async Task<IActionResult> OnGetAsync()
    {
        var playerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(playerIdClaim) || !Guid.TryParse(playerIdClaim, out var playerId))
        {
            return RedirectToPage("/Auth/Login");
        }

        var player = await _context.Players.FirstOrDefaultAsync(p => p.Id == playerId);
        if (player != null)
        {
            CurrentTier = player.CurrentTier.ToString();
        }

        Analytics = await _analyticsService.GetPlayerAnalyticsAsync(playerId);

        return Page();
    }
}
