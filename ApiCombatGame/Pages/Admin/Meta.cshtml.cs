using ApiCombatGame.Models.ViewModels;
using ApiCombatGame.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ApiCombatGame.Pages.Admin;

[Authorize(Policy = "Admin")]
public class MetaModel : PageModel
{
    private readonly IAdminAnalyticsService _analytics;

    public MetaModel(IAdminAnalyticsService analytics)
    {
        _analytics = analytics;
    }

    public AdminMetaData Data { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public int Days { get; set; } = 7;

    public async Task OnGetAsync()
    {
        Data = await _analytics.GetMetaDataAsync(Days);
    }
}
