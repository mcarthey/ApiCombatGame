namespace ApiCombatGame.Models.ViewModels;

public class BillingViewModel
{
    public string CurrentTier { get; set; } = "Free";
    public decimal MonthlyAmount { get; set; }
    public DateTime? NextBillingDate { get; set; }
    public string? PaymentMethodLast4 { get; set; }
    public string? PaymentMethodBrand { get; set; }
    public List<InvoiceInfo> RecentInvoices { get; set; } = new();
}

public class InvoiceInfo
{
    public string Id { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string? InvoiceUrl { get; set; }
}
