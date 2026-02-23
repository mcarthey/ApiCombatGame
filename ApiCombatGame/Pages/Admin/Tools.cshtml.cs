using System.Security.Claims;
using ApiCombatGame.Data;
using ApiCombatGame.Models.Enums;
using ApiCombatGame.Models.ViewModels;
using ApiCombatGame.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ApiCombatGame.Pages.Admin;

[Authorize(Policy = "Admin")]
public class ToolsModel : PageModel
{
    private readonly GameDbContext _context;
    private readonly IAdminReconciliationService _reconciliation;

    public ToolsModel(GameDbContext context, IAdminReconciliationService reconciliation)
    {
        _context = context;
        _reconciliation = reconciliation;
    }

    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }

    public int AdminCount { get; set; }
    public int TotalPlayers { get; set; }

    public ReconciliationPreview? Reconciliation { get; set; }
    public bool ShowReconciliationPreview { get; set; }

    [BindProperty]
    public DateTime? Since { get; set; }

    private Guid GetAdminId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (claim?.Value == null || !Guid.TryParse(claim.Value, out var adminId))
            throw new UnauthorizedAccessException("Admin identity claim missing.");
        return adminId;
    }

    public async Task OnGetAsync(string? message)
    {
        if (message == "success") SuccessMessage = "Action completed successfully.";
        if (message == "error") ErrorMessage = "Action failed. Check the logs.";

        AdminCount = await _context.Players.CountAsync(p => p.IsAdmin);
        TotalPlayers = await _context.Players.CountAsync();
    }

    public async Task<IActionResult> OnPostGrantAdminByUsernameAsync(string username)
    {
        var player = await _context.Players.FirstOrDefaultAsync(p => p.Username == username);
        if (player == null)
        {
            return RedirectToPage("Tools", new { message = "error" });
        }

        player.IsAdmin = true;
        player.AdminRole = AdminRole.SuperAdmin;
        await _context.SaveChangesAsync();

        return RedirectToPage("Tools", new { message = "success" });
    }

    public async Task<IActionResult> OnPostResetDatabaseAsync()
    {
        // Just recreate the database — deletes all data
        await _context.Database.EnsureDeletedAsync();
        await _context.Database.EnsureCreatedAsync();
        await SeedData.InitializeAsync(_context);
        await Phase3SeedData.InitializeAsync(_context);

        return RedirectToPage("Tools", new { message = "success" });
    }

    public async Task OnPostPreviewGlobalReconcileAsync()
    {
        Reconciliation = await _reconciliation.PreviewReconciliationAsync(Since);
        ShowReconciliationPreview = true;
        AdminCount = await _context.Players.CountAsync(p => p.IsAdmin);
        TotalPlayers = await _context.Players.CountAsync();
    }

    public async Task OnPostExecuteGlobalReconcileAsync()
    {
        var result = await _reconciliation.ExecuteReconciliationAsync(GetAdminId(), Since);
        SuccessMessage = $"Reconciliation complete: {result.TotalPlayersAffected} players corrected, " +
                         $"{result.TotalBattlesReprocessed} battles reprocessed, " +
                         $"{result.CasualBattlesFixed} casual battles fixed.";
        AdminCount = await _context.Players.CountAsync(p => p.IsAdmin);
        TotalPlayers = await _context.Players.CountAsync();
    }
}
