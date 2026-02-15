using ApiCombatGame.Models.Domain;
using ApiCombatGame.Models.Enums;

namespace ApiCombatGame.Services.Interfaces;

public interface ISubscriptionService
{
    Task<string> CreateCheckoutSessionAsync(Guid playerId, string tier, string baseUrl);
    Task ChangeTierAsync(Guid playerId, string newTier);
    Task HandleSubscriptionCreatedAsync(string stripeSubscriptionId, string customerId, string priceId, DateTime periodStart, DateTime periodEnd);
    Task HandleSubscriptionCanceledAsync(string stripeSubscriptionId);
    Task HandleSubscriptionUpdatedAsync(string stripeSubscriptionId, string priceId, string status, DateTime periodStart, DateTime periodEnd, DateTime? cancelAt = null);
    Task CancelSubscriptionAsync(Guid playerId);
    Task<Subscription?> GetSubscriptionAsync(Guid playerId);
    Task<string> CreateCustomerPortalSessionAsync(Guid playerId, string returnUrl);
}
