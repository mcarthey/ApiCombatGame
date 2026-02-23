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
    private readonly IAdminPlayerManagementService _management;

    public IndexModel(IAdminAnalyticsService analytics, IAdminPlayerManagementService management)
    {
        _analytics = analytics;
        _management = management;
    }

    public AdminOverviewData Data { get; set; } = new();
    public List<AdminAlert> ActiveAlerts { get; set; } = new();

    public async Task OnGetAsync()
    {
        Data = await _analytics.GetOverviewAsync();
        ActiveAlerts = await _management.GetActiveAlertsAsync();
    }

    public async Task<IActionResult> OnPostAcknowledgeAlertAsync(Guid alertId)
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (claim?.Value != null && Guid.TryParse(claim.Value, out var adminId))
            await _management.AcknowledgeAlertAsync(adminId, alertId);
        return RedirectToPage();
    }
}
