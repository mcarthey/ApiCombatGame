using ApiCombatGame.Middleware;
using Xunit;

namespace ApiCombatGame.Tests.Helpers;

/// <summary>
/// Shared base for integration tests that disables rate limiting.
/// </summary>
public static class IntegrationTestSetup
{
    public static void DisableRateLimiting()
    {
        RateLimitingMiddleware.Enabled = false;
    }
}
