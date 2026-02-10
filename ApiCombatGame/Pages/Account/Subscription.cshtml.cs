using System.Security.Claims;
using ApiCombatGame.Data;
using ApiCombatGame.Models.Enums;
using ApiCombatGame.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ApiCombatGame.Pages.Account;

[Authorize]
public class SubscriptionModel : PageModel
{
    private readonly GameDbContext _context;
    private readonly ISubscriptionService _subscriptionService;
    private readonly IConfiguration _config;
    private readonly ILogger<SubscriptionModel> _logger;

    public SubscriptionModel(
        GameDbContext context,
        ISubscriptionService subscriptionService,
        IConfiguration config,
        ILogger<SubscriptionModel> logger)
    {
        _context = context;
        _subscriptionService = subscriptionService;
        _config = config;
        _logger = logger;
    }

    public string CurrentTier { get; set; } = "Free";
    public bool ShowSuccessMessage { get; set; }
    public bool ShowCanceledMessage { get; set; }
    public DateTime? NextBillingDate { get; set; }
    public decimal MonthlyAmount { get; set; }
    public bool CanCancel { get; set; }

    public async Task OnGetAsync(bool? success, bool? canceled)
    {
        ShowSuccessMessage = success == true;
        ShowCanceledMessage = canceled == true;
        await LoadSubscriptionDataAsync();
    }

    public async Task<IActionResult> OnPostUpgradeAsync(string tier)
    {
        var playerId = GetPlayerId();
        try
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var checkoutUrl = await _subscriptionService.CreateCheckoutSessionAsync(playerId, tier, baseUrl);
            return Redirect(checkoutUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create checkout session for player {PlayerId}", playerId);
            await LoadSubscriptionDataAsync();
            return Page();
        }
    }

    public async Task<IActionResult> OnPostCancelAsync()
    {
        var playerId = GetPlayerId();
        try
        {
            await _subscriptionService.CancelSubscriptionAsync(playerId);
            return RedirectToPage(new { canceled = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel subscription for player {PlayerId}", playerId);
            await LoadSubscriptionDataAsync();
            return Page();
        }
    }

    public async Task<IActionResult> OnPostDowngradeAsync()
    {
        var playerId = GetPlayerId();
        try
        {
            await _subscriptionService.CancelSubscriptionAsync(playerId);
            return RedirectToPage(new { canceled = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to downgrade subscription for player {PlayerId}", playerId);
            await LoadSubscriptionDataAsync();
            return Page();
        }
    }

    private async Task LoadSubscriptionDataAsync()
    {
        var playerId = GetPlayerId();
        var player = await _context.Players.FindAsync(playerId);
        if (player == null) return;

        CurrentTier = player.CurrentTier.ToString();

        var sub = await _subscriptionService.GetSubscriptionAsync(playerId);
        if (sub != null && sub.Status == SubscriptionStatus.Active)
        {
            NextBillingDate = sub.CurrentPeriodEnd;
            MonthlyAmount = sub.AmountUsd;
            CanCancel = true;
        }
    }

    private Guid GetPlayerId()
    {
        var claim = User.FindFirst("PlayerId") ?? User.FindFirst(ClaimTypes.NameIdentifier);
        return Guid.Parse(claim!.Value);
    }
}
