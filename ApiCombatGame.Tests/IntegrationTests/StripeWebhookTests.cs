using System.Net;
using System.Text;
using ApiCombatGame.Controllers.Webhooks;
using ApiCombatGame.Data;
using ApiCombatGame.Services.Interfaces;
using ApiCombatGame.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ApiCombatGame.Tests.IntegrationTests;

/// <summary>
/// Tests for the Stripe webhook endpoint.
/// Uses direct controller invocation with mocked dependencies to verify
/// the controller correctly parses Stripe event JSON and delegates to the service.
/// The webhook secret is set to "whsec_test_webhook_secret" (dev mode)
/// so signature verification is bypassed.
/// </summary>
public class StripeWebhookTests
{
    private readonly Mock<ISubscriptionService> _mockSubscriptionService;
    private readonly StripeWebhookController _controller;

    public StripeWebhookTests()
    {
        _mockSubscriptionService = new Mock<ISubscriptionService>();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Stripe:WebhookSecret"] = "whsec_test_webhook_secret"
            })
            .Build();

        var logger = new Mock<ILogger<StripeWebhookController>>();

        _controller = new StripeWebhookController(
            _mockSubscriptionService.Object, config, logger.Object);
    }

    private void SetRequestBody(string json)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(json));
        httpContext.Request.ContentType = "application/json";
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    // ── Subscription Created ──

    [Fact]
    public async Task Webhook_SubscriptionCreated_CallsHandleCreated()
    {
        var json = BuildSubscriptionEvent("customer.subscription.created",
            "sub_test_created", "cus_test_1", "price_premium_test", "active");
        SetRequestBody(json);

        var result = await _controller.HandleWebhook();

        Assert.IsType<OkResult>(result);
        _mockSubscriptionService.Verify(s => s.HandleSubscriptionCreatedAsync(
            "sub_test_created", "cus_test_1", "price_premium_test",
            It.IsAny<DateTime>(), It.IsAny<DateTime>()), Times.Once);
    }

    [Fact]
    public async Task Webhook_SubscriptionCreated_PremiumPlus_CallsWithCorrectPriceId()
    {
        var json = BuildSubscriptionEvent("customer.subscription.created",
            "sub_plus_created", "cus_test_2", "price_plus_test", "active");
        SetRequestBody(json);

        var result = await _controller.HandleWebhook();

        Assert.IsType<OkResult>(result);
        _mockSubscriptionService.Verify(s => s.HandleSubscriptionCreatedAsync(
            "sub_plus_created", "cus_test_2", "price_plus_test",
            It.IsAny<DateTime>(), It.IsAny<DateTime>()), Times.Once);
    }

    // ── Subscription Updated ──

    [Fact]
    public async Task Webhook_SubscriptionUpdated_CallsHandleUpdated()
    {
        var json = BuildSubscriptionEvent("customer.subscription.updated",
            "sub_test_updated", "cus_test_3", "price_premium_test", "active");
        SetRequestBody(json);

        var result = await _controller.HandleWebhook();

        Assert.IsType<OkResult>(result);
        _mockSubscriptionService.Verify(s => s.HandleSubscriptionUpdatedAsync(
            "sub_test_updated", "price_premium_test", "active",
            It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<DateTime?>()), Times.Once);
    }

    [Fact]
    public async Task Webhook_SubscriptionUpdated_PastDue_PassesCorrectStatus()
    {
        var json = BuildSubscriptionEvent("customer.subscription.updated",
            "sub_pastdue", "cus_test_4", "price_premium_test", "past_due");
        SetRequestBody(json);

        var result = await _controller.HandleWebhook();

        Assert.IsType<OkResult>(result);
        _mockSubscriptionService.Verify(s => s.HandleSubscriptionUpdatedAsync(
            "sub_pastdue", "price_premium_test", "past_due",
            It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<DateTime?>()), Times.Once);
    }

    [Fact]
    public async Task Webhook_SubscriptionUpdated_TierUpgrade_PassesNewPriceId()
    {
        var json = BuildSubscriptionEvent("customer.subscription.updated",
            "sub_upgrade", "cus_test_5", "price_plus_test", "active");
        SetRequestBody(json);

        var result = await _controller.HandleWebhook();

        Assert.IsType<OkResult>(result);
        _mockSubscriptionService.Verify(s => s.HandleSubscriptionUpdatedAsync(
            "sub_upgrade", "price_plus_test", "active",
            It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<DateTime?>()), Times.Once);
    }

    // ── Subscription Deleted ──

    [Fact]
    public async Task Webhook_SubscriptionDeleted_CallsHandleCanceled()
    {
        var json = BuildSubscriptionEvent("customer.subscription.deleted",
            "sub_test_deleted", "cus_test_6", "price_premium_test", "canceled");
        SetRequestBody(json);

        var result = await _controller.HandleWebhook();

        Assert.IsType<OkResult>(result);
        _mockSubscriptionService.Verify(s => s.HandleSubscriptionCanceledAsync(
            "sub_test_deleted"), Times.Once);
    }

    // ── Invoice Events ──

    [Fact]
    public async Task Webhook_InvoicePaymentSucceeded_ReturnsOk()
    {
        var json = BuildInvoiceEvent("invoice.payment_succeeded", "in_test_1", "cus_test_7");
        SetRequestBody(json);

        var result = await _controller.HandleWebhook();

        Assert.IsType<OkResult>(result);
        _mockSubscriptionService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Webhook_InvoicePaymentFailed_ReturnsOk()
    {
        var json = BuildInvoiceEvent("invoice.payment_failed", "in_test_2", "cus_test_8");
        SetRequestBody(json);

        var result = await _controller.HandleWebhook();

        Assert.IsType<OkResult>(result);
        _mockSubscriptionService.VerifyNoOtherCalls();
    }

    // ── Unhandled Events ──

    [Fact]
    public async Task Webhook_UnhandledEventType_ReturnsOk()
    {
        var created = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var json = $$"""
        {
            "id": "evt_unknown",
            "object": "event",
            "api_version": "2023-10-16",
            "created": {{created}},
            "type": "charge.succeeded",
            "livemode": false,
            "pending_webhooks": 1,
            "request": { "id": "req_test", "idempotency_key": null },
            "data": {
                "object": {
                    "id": "ch_test",
                    "object": "charge"
                }
            }
        }
        """;
        SetRequestBody(json);

        var result = await _controller.HandleWebhook();

        Assert.IsType<OkResult>(result);
        _mockSubscriptionService.VerifyNoOtherCalls();
    }

    // ── Error Handling (Best Practice: never return 500 to Stripe) ──

    [Fact]
    public async Task Webhook_InvalidJson_ReturnsOk_NotServerError()
    {
        // Best practice: return 200 even for unparseable events to prevent
        // Stripe from endlessly retrying an event that will never succeed.
        SetRequestBody("not valid json at all {{{");

        var result = await _controller.HandleWebhook();

        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task Webhook_EmptyBody_ReturnsOk_NotServerError()
    {
        SetRequestBody("");

        var result = await _controller.HandleWebhook();

        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task Webhook_ServiceThrows_ReturnsOk_NotServerError()
    {
        // Best practice: if our own service fails (DB down, etc.), return 200
        // and log the error. Stripe retrying won't help if our DB is down.
        _mockSubscriptionService
            .Setup(s => s.HandleSubscriptionCreatedAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ThrowsAsync(new Exception("DB connection lost"));

        var json = BuildSubscriptionEvent("customer.subscription.created",
            "sub_error", "cus_error", "price_premium_test", "active");
        SetRequestBody(json);

        var result = await _controller.HandleWebhook();

        Assert.IsType<OkResult>(result);
    }

    // ── Null Deserialization (API version mismatch resilience) ──

    [Fact]
    public async Task Webhook_SubscriptionCreated_MissingItems_ReturnsOk_ServiceNotCalled()
    {
        // When API version mismatch causes incomplete deserialization,
        // the controller should handle null Items gracefully instead of 500.
        var created = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var json = $$"""
        {
            "id": "evt_missing_items",
            "object": "event",
            "api_version": "2023-10-16",
            "created": {{created}},
            "type": "customer.subscription.created",
            "livemode": false,
            "pending_webhooks": 1,
            "request": { "id": "req_test", "idempotency_key": null },
            "data": {
                "object": {
                    "id": "sub_no_items",
                    "object": "subscription",
                    "customer": "cus_test",
                    "status": "active",
                    "current_period_start": {{created}},
                    "current_period_end": {{created + 2592000}},
                    "items": {
                        "object": "list",
                        "data": []
                    },
                    "metadata": {}
                }
            }
        }
        """;
        SetRequestBody(json);

        var result = await _controller.HandleWebhook();

        Assert.IsType<OkResult>(result);
        _mockSubscriptionService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Webhook_SubscriptionUpdated_MissingPrice_ReturnsOk_ServiceNotCalled()
    {
        var created = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var json = $$"""
        {
            "id": "evt_missing_price",
            "object": "event",
            "api_version": "2023-10-16",
            "created": {{created}},
            "type": "customer.subscription.updated",
            "livemode": false,
            "pending_webhooks": 1,
            "request": { "id": "req_test", "idempotency_key": null },
            "data": {
                "object": {
                    "id": "sub_no_price",
                    "object": "subscription",
                    "customer": "cus_test",
                    "status": "active",
                    "current_period_start": {{created}},
                    "current_period_end": {{created + 2592000}},
                    "items": {
                        "object": "list",
                        "data": [
                            {
                                "id": "si_test",
                                "object": "subscription_item"
                            }
                        ]
                    },
                    "metadata": {}
                }
            }
        }
        """;
        SetRequestBody(json);

        var result = await _controller.HandleWebhook();

        Assert.IsType<OkResult>(result);
        _mockSubscriptionService.VerifyNoOtherCalls();
    }

    // ── Multiple Events ──

    [Fact]
    public async Task Webhook_MultipleEventsInSequence_AllProcessed()
    {
        // Create
        var json1 = BuildSubscriptionEvent("customer.subscription.created",
            "sub_seq_1", "cus_seq", "price_premium_test", "active");
        SetRequestBody(json1);
        var r1 = await _controller.HandleWebhook();
        Assert.IsType<OkResult>(r1);

        // Update (upgrade)
        var json2 = BuildSubscriptionEvent("customer.subscription.updated",
            "sub_seq_1", "cus_seq", "price_plus_test", "active");
        SetRequestBody(json2);
        var r2 = await _controller.HandleWebhook();
        Assert.IsType<OkResult>(r2);

        // Delete (cancel)
        var json3 = BuildSubscriptionEvent("customer.subscription.deleted",
            "sub_seq_1", "cus_seq", "price_plus_test", "canceled");
        SetRequestBody(json3);
        var r3 = await _controller.HandleWebhook();
        Assert.IsType<OkResult>(r3);

        _mockSubscriptionService.Verify(s => s.HandleSubscriptionCreatedAsync(
            "sub_seq_1", It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<DateTime>(), It.IsAny<DateTime>()), Times.Once);
        _mockSubscriptionService.Verify(s => s.HandleSubscriptionUpdatedAsync(
            "sub_seq_1", It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<DateTime?>()), Times.Once);
        _mockSubscriptionService.Verify(s => s.HandleSubscriptionCanceledAsync(
            "sub_seq_1"), Times.Once);
    }

    // ── Helpers ──

    private static string BuildSubscriptionEvent(string eventType, string subId,
        string customerId, string priceId, string status)
    {
        var periodStart = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var periodEnd = DateTimeOffset.UtcNow.AddMonths(1).ToUnixTimeSeconds();

        var created = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        return $$"""
        {
            "id": "evt_{{Guid.NewGuid():N}}",
            "object": "event",
            "api_version": "2023-10-16",
            "created": {{created}},
            "type": "{{eventType}}",
            "livemode": false,
            "pending_webhooks": 1,
            "request": { "id": "req_test", "idempotency_key": null },
            "data": {
                "object": {
                    "id": "{{subId}}",
                    "object": "subscription",
                    "customer": "{{customerId}}",
                    "status": "{{status}}",
                    "current_period_start": {{periodStart}},
                    "current_period_end": {{periodEnd}},
                    "items": {
                        "object": "list",
                        "data": [
                            {
                                "id": "si_test",
                                "object": "subscription_item",
                                "price": {
                                    "id": "{{priceId}}",
                                    "object": "price",
                                    "unit_amount": 499,
                                    "currency": "usd",
                                    "recurring": {
                                        "interval": "month"
                                    }
                                }
                            }
                        ]
                    },
                    "metadata": {}
                }
            }
        }
        """;
    }

    private static string BuildInvoiceEvent(string eventType, string invoiceId, string customerId)
    {
        var created = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        return $$"""
        {
            "id": "evt_{{Guid.NewGuid():N}}",
            "object": "event",
            "api_version": "2023-10-16",
            "created": {{created}},
            "type": "{{eventType}}",
            "livemode": false,
            "pending_webhooks": 1,
            "request": { "id": "req_test", "idempotency_key": null },
            "data": {
                "object": {
                    "id": "{{invoiceId}}",
                    "object": "invoice",
                    "customer": "{{customerId}}",
                    "amount_paid": 499,
                    "currency": "usd",
                    "status": "paid"
                }
            }
        }
        """;
    }
}
