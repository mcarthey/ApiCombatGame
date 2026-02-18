using System.Security.Claims;
using ApiCombatGame.Models.ViewModels;
using ApiCombatGame.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ApiCombatGame.Pages.Admin;

[Authorize(Policy = "Admin")]
public class PlayerDetailModel : PageModel
{
    private readonly IAdminAnalyticsService _analytics;
    private readonly IActivityLedger _ledger;

    public PlayerDetailModel(IAdminAnalyticsService analytics, IActivityLedger ledger)
    {
        _analytics = analytics;
        _ledger = ledger;
    }

    public AdminPlayerDetailData? PlayerData { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? LedgerProperty { get; set; }

    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }
    public PlayerReconciliationDelta? ReconciliationPreview { get; set; }
    public List<LedgerTimelineEntry> LedgerEntries { get; set; } = new();

    private Guid GetAdminId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    public async Task<IActionResult> OnGetAsync(string? message)
    {
        if (message == "updated") SuccessMessage = "Player updated successfully.";

        PlayerData = await _analytics.GetPlayerDetailAsync(Id);
        if (PlayerData == null) return RedirectToPage("Players");

        var entries = await _ledger.GetPlayerLedgerAsync(Id,
            string.IsNullOrEmpty(LedgerProperty) ? null : LedgerProperty, null, 50);
        LedgerEntries = entries.Select(e => new LedgerTimelineEntry
        {
            Timestamp = e.CreatedAt,
            Source = e.Source,
            Action = e.Action,
            Property = e.Property,
            OldValue = e.OldValue,
            NewValue = e.NewValue,
            Delta = e.Delta,
            RelatedEntityId = e.RelatedEntityId,
            ContextJson = e.ContextJson
        }).ToList();

        return Page();
    }

    public async Task<IActionResult> OnPostAdjustCurrencyAsync(Guid playerId, int amount)
    {
        await _analytics.AdjustCurrencyAsync(GetAdminId(), playerId, amount);
        return RedirectToPage("PlayerDetail", new { id = playerId, message = "updated" });
    }

    public async Task<IActionResult> OnPostAdjustRatingAsync(Guid playerId, int amount)
    {
        await _analytics.AdjustRatingAsync(GetAdminId(), playerId, amount);
        return RedirectToPage("PlayerDetail", new { id = playerId, message = "updated" });
    }

    public async Task<IActionResult> OnPostSetTierAsync(Guid playerId, string tier)
    {
        if (Enum.TryParse<ApiCombatGame.Models.Enums.SubscriptionTier>(tier, out var subscriptionTier))
        {
            await _analytics.SetTierAsync(GetAdminId(), playerId, subscriptionTier);
        }
        return RedirectToPage("PlayerDetail", new { id = playerId, message = "updated" });
    }

    public async Task<IActionResult> OnPostToggleAdminAsync(Guid playerId, bool makeAdmin)
    {
        await _analytics.ToggleAdminAsync(GetAdminId(), playerId, makeAdmin);
        return RedirectToPage("PlayerDetail", new { id = playerId, message = "updated" });
    }

    public async Task<IActionResult> OnPostResetPasswordAsync(Guid playerId, string newPassword)
    {
        if (!string.IsNullOrWhiteSpace(newPassword) && newPassword.Length >= 8)
        {
            await _analytics.ResetPasswordAsync(GetAdminId(), playerId, newPassword);
        }
        return RedirectToPage("PlayerDetail", new { id = playerId, message = "updated" });
    }

    public async Task<IActionResult> OnPostPreviewReconcileAsync(Guid playerId)
    {
        Id = playerId;
        PlayerData = await _analytics.GetPlayerDetailAsync(playerId);
        if (PlayerData == null) return RedirectToPage("Players");

        var preview = await _analytics.PreviewPlayerReconciliationAsync(playerId);
        ReconciliationPreview = preview.PlayerDeltas.FirstOrDefault();

        if (ReconciliationPreview == null || ReconciliationPreview.Delta == 0)
            SuccessMessage = "No rating discrepancy found for this player.";

        return Page();
    }
}
