using ApiCombatGame.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;

namespace ApiCombatGame.Controllers.Webhooks;

/// <summary>
/// Handles Stripe webhook events. Hidden from Swagger documentation.
/// </summary>
[ApiController]
[Route("api/webhooks/stripe")]
[ApiExplorerSettings(IgnoreApi = true)]
[AllowAnonymous]
public class StripeWebhookController : ControllerBase
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly IConfiguration _config;
    private readonly ILogger<StripeWebhookController> _logger;

    public StripeWebhookController(
        ISubscriptionService subscriptionService,
        IConfiguration config,
        ILogger<StripeWebhookController> logger)
    {
        _subscriptionService = subscriptionService;
        _config = config;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> HandleWebhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

        try
        {
            var webhookSecret = _config["Stripe:WebhookSecret"];
            Event stripeEvent;

            if (!string.IsNullOrEmpty(webhookSecret) && webhookSecret != "whsec_test_webhook_secret")
            {
                stripeEvent = EventUtility.ConstructEvent(
                    json,
                    Request.Headers["Stripe-Signature"],
                    webhookSecret
                );
            }
            else
            {
                // Development mode: parse without signature verification
                stripeEvent = EventUtility.ParseEvent(json);
            }

            _logger.LogInformation("Received Stripe webhook: {EventType}", stripeEvent.Type);

            switch (stripeEvent.Type)
            {
                case "customer.subscription.created":
                {
                    var subscription = stripeEvent.Data.Object as Subscription;
                    if (subscription != null)
                    {
                        await _subscriptionService.HandleSubscriptionCreatedAsync(
                            subscription.Id,
                            subscription.CustomerId,
                            subscription.Items.Data[0].Price.Id,
                            subscription.CurrentPeriodStart,
                            subscription.CurrentPeriodEnd);
                    }
                    break;
                }

                case "customer.subscription.updated":
                {
                    var subscription = stripeEvent.Data.Object as Subscription;
                    if (subscription != null)
                    {
                        await _subscriptionService.HandleSubscriptionUpdatedAsync(
                            subscription.Id,
                            subscription.Items.Data[0].Price.Id,
                            subscription.Status,
                            subscription.CurrentPeriodStart,
                            subscription.CurrentPeriodEnd);
                    }
                    break;
                }

                case "customer.subscription.deleted":
                {
                    var subscription = stripeEvent.Data.Object as Subscription;
                    if (subscription != null)
                    {
                        await _subscriptionService.HandleSubscriptionCanceledAsync(subscription.Id);
                    }
                    break;
                }

                case "invoice.payment_succeeded":
                {
                    var invoice = stripeEvent.Data.Object as Invoice;
                    _logger.LogInformation("Payment succeeded for customer {CustomerId}", invoice?.CustomerId);
                    break;
                }

                case "invoice.payment_failed":
                {
                    var invoice = stripeEvent.Data.Object as Invoice;
                    _logger.LogWarning("Payment failed for customer {CustomerId}", invoice?.CustomerId);
                    break;
                }

                default:
                    _logger.LogInformation("Unhandled Stripe event type: {EventType}", stripeEvent.Type);
                    break;
            }

            return Ok();
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe webhook signature verification failed");
            return BadRequest(new { error = "Webhook signature verification failed." });
        }
    }
}
