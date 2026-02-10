using System.Security.Claims;
using ApiCombatGame.Data;
using ApiCombatGame.Models.Enums;
using ApiCombatGame.Models.ViewModels;
using ApiCombatGame.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ApiCombatGame.Pages.Account;

[Authorize]
public class BillingModel : PageModel
{
    private readonly GameDbContext _context;
    private readonly ISubscriptionService _subscriptionService;
    private readonly IConfiguration _config;

    public BillingModel(GameDbContext context, ISubscriptionService subscriptionService, IConfiguration config)
    {
        _context = context;
        _subscriptionService = subscriptionService;
        _config = config;
    }

    public BillingViewModel Billing { get; set; } = new();

    public async Task OnGetAsync()
    {
        var playerId = GetPlayerId();
        var player = await _context.Players.FindAsync(playerId);
        if (player == null) return;

        Billing.CurrentTier = player.CurrentTier.ToString();

        var sub = await _subscriptionService.GetSubscriptionAsync(playerId);
        if (sub != null && sub.Status == SubscriptionStatus.Active)
        {
            Billing.MonthlyAmount = sub.AmountUsd;
            Billing.NextBillingDate = sub.CurrentPeriodEnd;
        }
    }

    public async Task<IActionResult> OnPostManageBillingAsync()
    {
        var playerId = GetPlayerId();
        try
        {
            var returnUrl = $"{Request.Scheme}://{Request.Host}/Account/Billing";
            var portalUrl = await _subscriptionService.CreateCustomerPortalSessionAsync(playerId, returnUrl);
            return Redirect(portalUrl);
        }
        catch (Exception)
        {
            return Page();
        }
    }

    private Guid GetPlayerId()
    {
        var claim = User.FindFirst("PlayerId") ?? User.FindFirst(ClaimTypes.NameIdentifier);
        return Guid.Parse(claim!.Value);
    }
}
