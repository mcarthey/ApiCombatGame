using ApiCombatGame.Models.ViewModels;
using ApiCombatGame.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ApiCombatGame.Pages.Admin;

[Authorize(Policy = "Admin")]
public class IndexModel : PageModel
{
    private readonly IAdminAnalyticsService _analytics;

    public IndexModel(IAdminAnalyticsService analytics)
    {
        _analytics = analytics;
    }

    public AdminOverviewData Data { get; set; } = new();

    public async Task OnGetAsync()
    {
        Data = await _analytics.GetOverviewAsync();
    }
}
