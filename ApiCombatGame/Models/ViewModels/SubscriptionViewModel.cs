namespace ApiCombatGame.Models.ViewModels;

public class SubscriptionViewModel
{
    public string CurrentTier { get; set; } = "Free";
    public bool ShowSuccessMessage { get; set; }
    public bool ShowCanceledMessage { get; set; }
    public DateTime? NextBillingDate { get; set; }
    public decimal MonthlyAmount { get; set; }
    public bool CanCancel { get; set; }
    public string? StripePublishableKey { get; set; }
}
