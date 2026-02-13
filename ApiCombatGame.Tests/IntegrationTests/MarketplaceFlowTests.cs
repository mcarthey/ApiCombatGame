using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ApiCombatGame.Models.DTOs.Marketplace;
using ApiCombatGame.Tests.Helpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ApiCombatGame.Tests.IntegrationTests;

public class MarketplaceFlowTests : IntegrationTestBase
{
    public MarketplaceFlowTests(WebApplicationFactory<Program> factory)
        : base(factory, "Marketplace") { }

    [Fact]
    public async Task BrowseStrategies_NoAuth_ReturnsOk()
    {
        // Arrange
        var client = CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/strategies/browse");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var strategies = JsonSerializer.Deserialize<List<StrategyResponse>>(content, JsonOptions);
        Assert.NotNull(strategies);
    }

    [Fact]
    public async Task BrowseStrategies_WithParams_ReturnsOk()
    {
        // Arrange
        var client = CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/strategies/browse?sortBy=newest&limit=5&offset=0");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UploadStrategy_ValidRequest_ReturnsCreated()
    {
        // Arrange
        var (client, _) = await CreateAuthenticatedClient();

        var uploadRequest = new StrategyUploadRequest
        {
            Name = "Test Strat",
            Description = "A test",
            StrategyJson = "{\"formation\":\"defensive\"}",
            Price = 0
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/strategies/upload", uploadRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var strategy = JsonSerializer.Deserialize<StrategyResponse>(content, JsonOptions);
        Assert.NotNull(strategy);
        Assert.Equal("Test Strat", strategy!.Name);
    }

    [Fact]
    public async Task UploadStrategy_MissingName_ReturnsBadRequest()
    {
        // Arrange
        var (client, _) = await CreateAuthenticatedClient();

        var uploadRequest = new StrategyUploadRequest
        {
            Name = "",
            Description = "A test with no name",
            StrategyJson = "{}",
            Price = 0
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/strategies/upload", uploadRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DownloadStrategy_NonExistent_ReturnsNotFound()
    {
        // Arrange
        var (client, _) = await CreateAuthenticatedClient();
        var randomId = Guid.NewGuid();

        // Act
        var response = await client.PostAsync($"/api/v1/strategies/{randomId}/download", null);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task RateStrategy_NonExistent_ReturnsNotFound()
    {
        // Arrange
        var (client, _) = await CreateAuthenticatedClient();
        var randomId = Guid.NewGuid();

        var ratingRequest = new StrategyRatingRequest
        {
            Rating = 5,
            Comment = "Great strategy"
        };

        // Act
        var response = await client.PostAsJsonAsync($"/api/v1/strategies/{randomId}/rate", ratingRequest);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task MarketplaceFlow_UploadThenBrowse_StrategyAppears()
    {
        // Arrange
        var (client, _) = await CreateAuthenticatedClient();

        var strategyName = $"FlowTest_{Guid.NewGuid():N}";
        var uploadRequest = new StrategyUploadRequest
        {
            Name = strategyName,
            Description = "Integration test strategy",
            StrategyJson = "{\"formation\":\"balanced\"}",
            Price = 0
        };

        // Act - Upload
        var uploadResponse = await client.PostAsJsonAsync("/api/v1/strategies/upload", uploadRequest);
        Assert.Equal(HttpStatusCode.Created, uploadResponse.StatusCode);

        // Act - Browse
        var browseResponse = await client.GetAsync("/api/v1/strategies/browse");
        Assert.Equal(HttpStatusCode.OK, browseResponse.StatusCode);

        // Assert - Strategy appears in browse results
        var content = await browseResponse.Content.ReadAsStringAsync();
        var strategies = JsonSerializer.Deserialize<List<StrategyResponse>>(content, JsonOptions);
        Assert.NotNull(strategies);

        var uploadedStrategy = strategies!.FirstOrDefault(s => s.Name == strategyName);
        Assert.NotNull(uploadedStrategy);
        Assert.Equal(strategyName, uploadedStrategy!.Name);
        Assert.Equal(0, uploadedStrategy.Price);
    }
}
