using ApiCombatGame.Models.ViewModels;
using ApiCombatGame.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ApiCombatGame.Pages.Admin;

[Authorize(Policy = "Admin")]
public class PlayersModel : PageModel
{
    private readonly IAdminAnalyticsService _analytics;

    public PlayersModel(IAdminAnalyticsService analytics)
    {
        _analytics = analytics;
    }

    public AdminPlayerAnalyticsData Data { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Tier { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool HideBots { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SortBy { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool SortDesc { get; set; } = true;

    [BindProperty(SupportsGet = true)]
    public new int Page { get; set; } = 1;

    /// <summary>Builds query string preserving all current filters.</summary>
    public string FilterQuery => string.Join("&",
        new[] {
            string.IsNullOrEmpty(Search) ? null : $"Search={Uri.EscapeDataString(Search)}",
            string.IsNullOrEmpty(Tier) ? null : $"Tier={Tier}",
            HideBots ? "HideBots=true" : null,
        }.Where(s => s != null));

    public async Task OnGetAsync()
    {
        Data = await _analytics.GetPlayerAnalyticsAsync(Search, Tier, Page, hideBots: HideBots, sortBy: SortBy, sortDesc: SortDesc);
    }
}
