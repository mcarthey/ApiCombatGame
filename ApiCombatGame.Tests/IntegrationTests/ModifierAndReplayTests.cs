using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ApiCombatGame.Models.DTOs.Progression;
using ApiCombatGame.Tests.Helpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ApiCombatGame.Tests.IntegrationTests;

public class ModifierAndReplayTests : IntegrationTestBase
{
    public ModifierAndReplayTests(WebApplicationFactory<Program> factory)
        : base(factory, "ModifierReplay")
    {
    }

    [Fact]
    public async Task GetCurrentModifier_NoAuth_ReturnsOk()
    {
        var client = CreateClient();

        var response = await client.GetAsync("/api/v1/modifiers/current");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var modifier = JsonSerializer.Deserialize<JsonElement>(content, JsonOptions);
        Assert.True(modifier.TryGetProperty("name", out _), "Response should contain a 'name' property");
    }

    [Fact]
    public async Task GetCurrentModifier_ReturnsValidModifierData()
    {
        var client = CreateClient();

        var response = await client.GetAsync("/api/v1/modifiers/current");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var modifier = JsonSerializer.Deserialize<JsonElement>(content, JsonOptions);
        var name = modifier.GetProperty("name").GetString();
        Assert.False(string.IsNullOrEmpty(name), "Modifier should have a non-empty name");
    }

    [Fact]
    public async Task GetUpcomingModifier_ReturnsOkOrNotFound()
    {
        var client = CreateClient();

        var response = await client.GetAsync("/api/v1/modifiers/upcoming");

        // Seed data may or may not include an upcoming modifier
        Assert.True(
            response.StatusCode is HttpStatusCode.OK or HttpStatusCode.NotFound,
            $"Expected OK or NotFound but got {response.StatusCode}");
    }

    [Fact]
    public async Task CreateReplay_NonExistentBattle_ReturnsNotFound()
    {
        var (client, _) = await CreateAuthenticatedClient();

        var response = await client.PostAsJsonAsync("/api/v1/replays/create", new
        {
            battleId = Guid.NewGuid()
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetReplay_NonExistentShareUrl_ReturnsNotFound()
    {
        var client = CreateClient();

        var response = await client.GetAsync("/api/v1/replays/nonexistent123");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
