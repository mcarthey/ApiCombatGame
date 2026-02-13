using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ApiCombatGame.Tests.Helpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ApiCombatGame.Tests.IntegrationTests;

public class ChallengeAndMasteryTests : IntegrationTestBase
{
    public ChallengeAndMasteryTests(WebApplicationFactory<Program> factory)
        : base(factory, "ChallengeMastery")
    {
    }

    [Fact]
    public async Task GetDailyChallenges_Authenticated_ReturnsOkArray()
    {
        var (client, _) = await CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/v1/challenges/daily");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var challenges = JsonSerializer.Deserialize<JsonElement>(content, JsonOptions);
        Assert.Equal(JsonValueKind.Array, challenges.ValueKind);
    }

    [Fact]
    public async Task ClaimChallenge_NonExistent_ReturnsNotFound()
    {
        var (client, _) = await CreateAuthenticatedClient();

        var response = await client.PostAsJsonAsync("/api/v1/challenges/claim", new
        {
            challengeId = Guid.NewGuid()
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetMasteryUnits_NewPlayer_ReturnsOkEmptyArray()
    {
        var (client, _) = await CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/v1/mastery/units");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var masteryUnits = JsonSerializer.Deserialize<JsonElement>(content, JsonOptions);
        Assert.Equal(JsonValueKind.Array, masteryUnits.ValueKind);
    }

    [Fact]
    public async Task GetUnitMastery_AnyUnit_ReturnsDefaultMastery()
    {
        var (client, _) = await CreateAuthenticatedClient();
        var unitId = Guid.NewGuid();

        var response = await client.GetAsync($"/api/v1/mastery/unit/{unitId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var mastery = JsonSerializer.Deserialize<JsonElement>(content, JsonOptions);

        Assert.Equal(1, mastery.GetProperty("level").GetInt32());
        Assert.Equal(0, mastery.GetProperty("experiencePoints").GetInt32());
    }

    [Fact]
    public async Task GetDailyChallenges_Unauthenticated_ReturnsUnauthorized()
    {
        var client = CreateClient();

        var response = await client.GetAsync("/api/v1/challenges/daily");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMastery_Unauthenticated_ReturnsUnauthorized()
    {
        var client = CreateClient();

        var response = await client.GetAsync("/api/v1/mastery/units");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
