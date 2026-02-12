using System.Collections.Concurrent;
using System.Security.Claims;

namespace ApiCombatGame.Middleware;

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private static readonly ConcurrentDictionary<string, ClientRateInfo> Clients = new();
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

    /// <summary>Set to false in integration tests to disable rate limiting.</summary>
    public static bool Enabled { get; set; } = true;

    // Tier-based rate limits per minute
    private const int FreeLimit = 60;
    private const int PremiumLimit = 120;
    private const int PremiumPlusLimit = 300;

    public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!Enabled)
        {
            await _next(context);
            return;
        }

        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        // Determine rate limit based on subscription tier from JWT claims
        var tierClaim = context.User?.FindFirst("CurrentTier")?.Value;
        int maxRequests = tierClaim switch
        {
            "PremiumPlus" => PremiumPlusLimit,
            "Premium" => PremiumLimit,
            _ => FreeLimit
        };

        // Use IP + tier as key so upgraded users get their own bucket
        var clientKey = $"{clientIp}:{tierClaim ?? "Free"}";
        var clientInfo = Clients.GetOrAdd(clientKey, _ => new ClientRateInfo());

        lock (clientInfo)
        {
            var now = DateTime.UtcNow;

            // Reset window if expired
            if (now - clientInfo.WindowStart >= Window)
            {
                clientInfo.WindowStart = now;
                clientInfo.RequestCount = 0;
            }

            clientInfo.RequestCount++;

            var remaining = Math.Max(0, maxRequests - clientInfo.RequestCount);
            var resetTime = clientInfo.WindowStart.Add(Window);

            context.Response.Headers["X-RateLimit-Limit"] = maxRequests.ToString();
            context.Response.Headers["X-RateLimit-Remaining"] = remaining.ToString();
            context.Response.Headers["X-RateLimit-Reset"] = new DateTimeOffset(resetTime).ToUnixTimeSeconds().ToString();

            if (clientInfo.RequestCount > maxRequests)
            {
                _logger.LogWarning("Rate limit exceeded for {ClientKey} ({Limit}/min)", clientKey, maxRequests);
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.Response.Headers["Retry-After"] = ((int)(resetTime - now).TotalSeconds).ToString();
                context.Response.ContentType = "application/json";
                var body = System.Text.Json.JsonSerializer.Serialize(new
                {
                    error = "Rate limit exceeded. Try again later.",
                    limit = maxRequests,
                    tier = tierClaim ?? "Free",
                    retryAfterSeconds = (int)(resetTime - now).TotalSeconds
                });
                context.Response.WriteAsync(body).Wait();
                return;
            }
        }

        await _next(context);

        // Periodic cleanup of stale entries
        if (Random.Shared.NextDouble() < 0.01)
        {
            CleanupStaleEntries();
        }
    }

    private static void CleanupStaleEntries()
    {
        var cutoff = DateTime.UtcNow - TimeSpan.FromMinutes(5);
        foreach (var kvp in Clients)
        {
            if (kvp.Value.WindowStart < cutoff)
            {
                Clients.TryRemove(kvp.Key, out _);
            }
        }
    }

    private class ClientRateInfo
    {
        public DateTime WindowStart { get; set; } = DateTime.UtcNow;
        public int RequestCount { get; set; }
    }
}
