using System.Net;
using System.Text.Json;
using ApiCombatGame.Tests.Helpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ApiCombatGame.Tests.IntegrationTests;

public class LinkVerificationTests : IntegrationTestBase
{
    public LinkVerificationTests(WebApplicationFactory<Program> factory)
        : base(factory, "Links")
    {
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/Privacy")]
    [InlineData("/Auth/Login")]
    [InlineData("/Auth/Register")]
    public async Task PublicPages_ReturnSuccessStatusCode(string url)
    {
        // Arrange
        var client = CreateClient();

        // Act
        var response = await client.GetAsync(url);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ApiDocsPage_ReturnsSuccess()
    {
        // Arrange
        var client = CreateClient();

        // Act
        var response = await client.GetAsync("/api-docs/v1");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task HealthEndpoint_ReturnsHealthy()
    {
        // Arrange
        var client = CreateClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task OpenApiSpec_ReturnsValidJson()
    {
        // Arrange
        var client = CreateClient();

        // Act
        var response = await client.GetAsync("/openapi/v1.json");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(content);
        Assert.NotEqual(default, json);
    }

    [Theory]
    [InlineData("/Dashboard")]
    [InlineData("/Account")]
    [InlineData("/Account/Subscription")]
    [InlineData("/Account/Billing")]
    [InlineData("/Account/ApiKeys")]
    [InlineData("/Account/Settings")]
    [InlineData("/Account/Notifications")]
    public async Task AuthenticatedPages_RedirectWhenNotAuthenticated(string url)
    {
        // Arrange
        var client = CreateClientNoRedirect();

        // Act
        var response = await client.GetAsync(url);

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Auth/Login", response.Headers.Location?.ToString());
    }

    [Theory]
    [InlineData("/api/v1/player/profile")]
    [InlineData("/api/v1/player/roster")]
    [InlineData("/api/v1/team/list")]
    [InlineData("/api/v1/challenges/daily")]
    [InlineData("/api/v1/mastery/units")]
    public async Task ProtectedApiEndpoints_ReturnUnauthorized_WithoutToken(string url)
    {
        // Arrange
        var client = CreateClient();

        // Act
        var response = await client.GetAsync(url);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/v1/modifiers/current")]
    [InlineData("/api/v1/strategies/browse")]
    public async Task PublicApiEndpoints_ReturnSuccess_WithoutToken(string url)
    {
        // Arrange
        var client = CreateClient();

        // Act
        var response = await client.GetAsync(url);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/v1/player/profile")]
    [InlineData("/api/v1/player/roster")]
    [InlineData("/api/v1/team/list")]
    [InlineData("/api/v1/challenges/daily")]
    [InlineData("/api/v1/mastery/units")]
    public async Task AuthenticatedApiEndpoints_ReturnSuccess_WithToken(string url)
    {
        // Arrange
        var (client, _) = await CreateAuthenticatedClient();

        // Act
        var response = await client.GetAsync(url);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
