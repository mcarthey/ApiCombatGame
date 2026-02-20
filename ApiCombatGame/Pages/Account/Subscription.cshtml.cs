using System.Security.Claims;
using ApiCombatGame.Data;
using ApiCombatGame.Models.Enums;
using ApiCombatGame.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Stripe;

namespace ApiCombatGame.Pages.Account;

[Authorize]
public class SubscriptionModel : PageModel
{
    private readonly GameDbContext _context;
    private readonly ISubscriptionService _subscriptionService;
    private readonly IAuthService _authService;
    private readonly IConfiguration _config;
    private readonly ILogger<SubscriptionModel> _logger;

    public SubscriptionModel(
        GameDbContext context,
        ISubscriptionService subscriptionService,
        IAuthService authService,
        IConfiguration config,
        ILogger<SubscriptionModel> logger)
    {
        _context = context;
        _subscriptionService = subscriptionService;
        _authService = authService;
        _config = config;
        _logger = logger;
    }

    public string CurrentTier { get; set; } = "Free";
    public bool ShowSuccessMessage { get; set; }
    public bool ShowProcessingMessage { get; set; }
    public bool ShowCanceledMessage { get; set; }
    public DateTime? NextBillingDate { get; set; }
    public decimal MonthlyAmount { get; set; }
    public bool CanCancel { get; set; }
    public bool IsCancellationPending { get; set; }
    public DateTime? ExpiresOn { get; set; }
    public DateTime? CurrentPeriodStart { get; set; }
    public bool EmailVerified { get; set; }
    public bool IsEnrolledStudent { get; set; }
    public bool CanPurchase => EmailVerified && !IsEnrolledStudent;
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync(bool? success, bool? canceled)
    {
        ShowCanceledMessage = canceled == true;
        await LoadSubscriptionDataAsync();

        // Only show success if the player's tier actually upgraded.
        // If Stripe redirected with ?success=true but the webhook hasn't
        // been processed yet, show a "processing" message instead.
        if (success == true)
        {
            if (CurrentTier != "Free")
                ShowSuccessMessage = true;
            else
                ShowProcessingMessage = true;
        }
    }

    public async Task<IActionResult> OnPostUpgradeAsync(string tier)
    {
        var playerId = GetPlayerId();
        try
        {
            // If player already has an active Stripe subscription, change tier directly
            // (handles proration automatically). Otherwise create a new Checkout Session.
            var existingSub = await _subscriptionService.GetSubscriptionAsync(playerId);
            if (existingSub != null && !string.IsNullOrEmpty(existingSub.StripeSubscriptionId)
                && existingSub.Status == Models.Enums.SubscriptionStatus.Active)
            {
                await _subscriptionService.ChangeTierAsync(playerId, tier);
                return RedirectToPage(new { success = true });
            }

            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var checkoutUrl = await _subscriptionService.CreateCheckoutSessionAsync(playerId, tier, baseUrl);
            return Redirect(checkoutUrl);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Upgrade blocked for player {PlayerId}: {Reason}", playerId, ex.Message);
            await LoadSubscriptionDataAsync();
            ErrorMessage = ex.Message;
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upgrade subscription for player {PlayerId}", playerId);
            await LoadSubscriptionDataAsync();
            ErrorMessage = "Something went wrong. Please try again later.";
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

    public async Task<IActionResult> OnPostResendVerificationAsync()
    {
        var playerId = GetPlayerId();
        await _authService.SendVerificationEmailAsync(playerId);
        TempData["VerificationResent"] = true;
        return RedirectToPage();
    }

    private async Task LoadSubscriptionDataAsync()
    {
        var playerId = GetPlayerId();
        var player = await _context.Players.FindAsync(playerId);
        if (player == null) return;

        CurrentTier = player.CurrentTier.ToString();
        EmailVerified = player.EmailConfirmed;
        IsEnrolledStudent = await _context.StudentEnrollments
            .AnyAsync(e => e.PlayerId == playerId && !e.IsCompleted);

        var sub = await _subscriptionService.GetSubscriptionAsync(playerId);
        if (sub != null && sub.Status == SubscriptionStatus.Active)
        {
            MonthlyAmount = sub.AmountUsd;

            var minValid = new DateTime(2020, 1, 1);

            // Self-heal: if DB dates are bad, fetch authoritative dates from Stripe API
            if ((sub.CurrentPeriodStart <= minValid || sub.CurrentPeriodEnd <= minValid)
                && !string.IsNullOrEmpty(sub.StripeSubscriptionId))
            {
                try
                {
                    var stripeService = new Stripe.SubscriptionService();
                    var stripeSub = await stripeService.GetAsync(sub.StripeSubscriptionId);

                    if (stripeSub.CurrentPeriodStart > minValid)
                        sub.CurrentPeriodStart = stripeSub.CurrentPeriodStart;
                    if (stripeSub.CurrentPeriodEnd > minValid)
                        sub.CurrentPeriodEnd = stripeSub.CurrentPeriodEnd;
                    if (stripeSub.Items.Data.FirstOrDefault()?.Price?.UnitAmount is long unitAmount)
                        sub.AmountUsd = unitAmount / 100m;

                    // Persist the corrected dates so future loads don't need the API call
                    await _context.SaveChangesAsync();
                    MonthlyAmount = sub.AmountUsd;

                    _logger.LogInformation("Self-healed subscription dates from Stripe for player {PlayerId}: {Start} – {End}",
                        playerId, sub.CurrentPeriodStart, sub.CurrentPeriodEnd);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to refresh subscription dates from Stripe for player {PlayerId}", playerId);
                }
            }

            var periodStart = sub.CurrentPeriodStart > minValid ? sub.CurrentPeriodStart : (DateTime?)null;
            var periodEnd = sub.CurrentPeriodEnd > minValid ? sub.CurrentPeriodEnd : (DateTime?)null;

            CurrentPeriodStart = periodStart;

            if (sub.CancelAt.HasValue)
            {
                IsCancellationPending = true;
                ExpiresOn = sub.CancelAt.Value > minValid
                    ? sub.CancelAt.Value
                    : periodEnd;
                CanCancel = false; // Already scheduled
            }
            else
            {
                NextBillingDate = periodEnd;
                CanCancel = true;
            }
        }
    }

    private Guid GetPlayerId()
    {
        var claim = User.FindFirst("PlayerId") ?? User.FindFirst(ClaimTypes.NameIdentifier);
        if (claim?.Value == null || !Guid.TryParse(claim.Value, out var playerId))
            throw new UnauthorizedAccessException("PlayerId claim not found.");
        return playerId;
    }
}
