using System.Net;
using System.Net.Http.Json;
using ApiCombatGame.Data;
using ApiCombatGame.Services.Interfaces;
using ApiCombatGame.Tests.Helpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ApiCombatGame.Tests.IntegrationTests;

public class ApiKeyAuthIntegrationTests : IntegrationTestBase
{
    public ApiKeyAuthIntegrationTests(WebApplicationFactory<Program> factory)
        : base(factory, "ApiKeyAuth") { }

    [Fact]
    public async Task NoApiKeyHeader_FallsThrough_ReturnsUnauthorized()
    {
        var client = CreateClient();

        // No auth headers at all → 401
        var response = await client.GetAsync("/api/v1/player/roster");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task InvalidApiKey_Returns401()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "acg_totally_bogus_key");

        var response = await client.GetAsync("/api/v1/player/roster");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ValidApiKey_AuthenticatesAndReturns200()
    {
        // Register a player to get them in the DB
        var (jwtClient, auth) = await CreateAuthenticatedClient();

        // Create an API key for the player via the service
        using var scope = Factory.Services.CreateScope();
        var apiKeyService = scope.ServiceProvider.GetRequiredService<IApiKeyService>();
        var (_, plainTextKey) = await apiKeyService.CreateApiKeyAsync(auth.PlayerId, "Integration Test Key");

        // Use API key auth (not JWT)
        var apiKeyClient = CreateClient();
        apiKeyClient.DefaultRequestHeaders.Add("X-Api-Key", plainTextKey);

        var response = await apiKeyClient.GetAsync("/api/v1/player/roster");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ValidApiKey_LogsUsage()
    {
        var (jwtClient, auth) = await CreateAuthenticatedClient();

        using var scope = Factory.Services.CreateScope();
        var apiKeyService = scope.ServiceProvider.GetRequiredService<IApiKeyService>();
        var (key, plainTextKey) = await apiKeyService.CreateApiKeyAsync(auth.PlayerId, "Usage Log Test");

        var apiKeyClient = CreateClient();
        apiKeyClient.DefaultRequestHeaders.Add("X-Api-Key", plainTextKey);

        await apiKeyClient.GetAsync("/api/v1/player/roster");

        // Verify usage was logged
        var context = scope.ServiceProvider.GetRequiredService<GameDbContext>();
        var usageLogs = await context.ApiKeyUsageLogs
            .Where(l => l.ApiKeyId == key.Id)
            .ToListAsync();

        Assert.NotEmpty(usageLogs);
        Assert.Contains(usageLogs, l => l.Endpoint == "/api/v1/player/roster");
    }
}
