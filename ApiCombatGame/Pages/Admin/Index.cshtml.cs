using System.Security.Claims;
using ApiCombatGame.Models.Domain;
using ApiCombatGame.Models.ViewModels;
using ApiCombatGame.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    public List<AdminAlert> ActiveAlerts { get; set; } = new();

    public async Task OnGetAsync()
    {
        Data = await _analytics.GetOverviewAsync();
        ActiveAlerts = await _analytics.GetActiveAlertsAsync();
    }

    public async Task<IActionResult> OnPostAcknowledgeAlertAsync(Guid alertId)
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (claim?.Value != null && Guid.TryParse(claim.Value, out var adminId))
            await _analytics.AcknowledgeAlertAsync(adminId, alertId);
        return RedirectToPage();
    }
}
