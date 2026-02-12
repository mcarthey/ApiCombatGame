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

    public PlayerDetailModel(IAdminAnalyticsService analytics)
    {
        _analytics = analytics;
    }

    public AdminPlayerDetailData? PlayerData { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }

    private Guid GetAdminId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    public async Task<IActionResult> OnGetAsync(string? message)
    {
        if (message == "updated") SuccessMessage = "Player updated successfully.";

        PlayerData = await _analytics.GetPlayerDetailAsync(Id);
        if (PlayerData == null) return RedirectToPage("Players");
        return Page();
    }

    public async Task<IActionResult> OnPostAdjustCurrencyAsync(Guid playerId, int amount)
    {
        await _analytics.AdjustCurrencyAsync(GetAdminId(), playerId, amount);
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
}
