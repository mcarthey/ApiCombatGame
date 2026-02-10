using System.Collections.Concurrent;

namespace ApiCombatGame.Middleware;

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private static readonly ConcurrentDictionary<string, ClientRateInfo> Clients = new();
    private const int MaxRequestsPerMinute = 60;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

    public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        var clientInfo = Clients.GetOrAdd(clientIp, _ => new ClientRateInfo());

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

            var remaining = Math.Max(0, MaxRequestsPerMinute - clientInfo.RequestCount);
            var resetTime = clientInfo.WindowStart.Add(Window);

            context.Response.Headers["X-RateLimit-Limit"] = MaxRequestsPerMinute.ToString();
            context.Response.Headers["X-RateLimit-Remaining"] = remaining.ToString();
            context.Response.Headers["X-RateLimit-Reset"] = new DateTimeOffset(resetTime).ToUnixTimeSeconds().ToString();

            if (clientInfo.RequestCount > MaxRequestsPerMinute)
            {
                _logger.LogWarning("Rate limit exceeded for IP: {ClientIp}", clientIp);
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.Response.Headers["Retry-After"] = ((int)(resetTime - now).TotalSeconds).ToString();
                context.Response.ContentType = "application/json";
                var body = System.Text.Json.JsonSerializer.Serialize(new { error = "Rate limit exceeded. Try again later." });
                context.Response.WriteAsync(body).Wait();
                return;
            }
        }

        await _next(context);

        // Periodic cleanup of stale entries
        if (Random.Shared.NextDouble() < 0.01) // 1% chance per request
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
