using ApiCombatGame.Models.ViewModels;
using ApiCombatGame.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ApiCombatGame.Pages.Admin;

[Authorize(Policy = "Admin")]
public class AuditLogModel : PageModel
{
    private readonly IAdminPlayerManagementService _management;

    public AuditLogModel(IAdminPlayerManagementService management)
    {
        _management = management;
    }

    public AdminAuditLogData Data { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? ActionFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public int CurrentPage { get; set; } = 1;

    public async Task OnGetAsync()
    {
        if (CurrentPage < 1) CurrentPage = 1;
        Data = await _management.GetAuditLogsAsync(ActionFilter, CurrentPage);
    }
}
