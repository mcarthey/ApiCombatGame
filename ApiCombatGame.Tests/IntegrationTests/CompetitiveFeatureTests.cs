using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ApiCombatGame.Tests.Helpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ApiCombatGame.Tests.IntegrationTests;

/// <summary>
/// Integration tests for competitive features:
/// AI Practice, Ranked Seasons, Leaderboard, Tournaments, Guild Wars.
/// </summary>
public class CompetitiveFeatureTests : IntegrationTestBase
{
    public CompetitiveFeatureTests(WebApplicationFactory<Program> factory)
        : base(factory, "Competitive") { }

    // ── AI Practice ──

    [Fact]
    public async Task AiOpponents_GetList_ReturnsOkWithPresets()
    {
        var (client, _) = await CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/v1/ai/opponents");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(content, JsonOptions);
        Assert.Equal(JsonValueKind.Object, json.ValueKind);
        Assert.True(json.GetProperty("opponents").GetArrayLength() > 0);
    }

    [Fact]
    public async Task AiPractice_NoTeam_ReturnsBadRequestOrNotFound()
    {
        var (client, _) = await CreateAuthenticatedClient();

        var request = new { teamId = Guid.NewGuid(), opponentId = "novice_1" };
        var response = await client.PostAsJsonAsync("/api/v1/ai/practice", request);

        // Should fail because team doesn't exist
        Assert.True(
            response.StatusCode == HttpStatusCode.BadRequest ||
            response.StatusCode == HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AiOpponents_Unauthenticated_ReturnsUnauthorized()
    {
        var client = CreateClient();

        var response = await client.GetAsync("/api/v1/ai/opponents");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── Ranked Seasons ──

    [Fact]
    public async Task Season_GetCurrent_ReturnsOkWithSeasonData()
    {
        var (client, _) = await CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/v1/season/current");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(content, JsonOptions);
        Assert.True(json.TryGetProperty("season", out _) || json.TryGetProperty("seasonId", out _) || json.TryGetProperty("tier", out _));
    }

    [Fact]
    public async Task Season_GetLeaderboard_ReturnsOkWithRankings()
    {
        var (client, _) = await CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/v1/season/leaderboard");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(content, JsonOptions);
        Assert.True(json.TryGetProperty("rankings", out _) || json.TryGetProperty("seasonName", out _));
    }

    [Fact]
    public async Task Season_ClaimRewards_NonExistentSeason_ReturnsNotFoundOrBadRequest()
    {
        var (client, _) = await CreateAuthenticatedClient();

        var response = await client.PostAsync($"/api/v1/season/rewards/{Guid.NewGuid()}", null);

        Assert.True(
            response.StatusCode == HttpStatusCode.NotFound ||
            response.StatusCode == HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Season_Unauthenticated_ReturnsUnauthorized()
    {
        var client = CreateClient();

        var response = await client.GetAsync("/api/v1/season/current");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── Leaderboard ──

    [Fact]
    public async Task Leaderboard_GetGlobal_ReturnsOk()
    {
        var (client, _) = await CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/v1/leaderboard");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(content, JsonOptions);
        Assert.Equal(JsonValueKind.Array, json.ValueKind);
    }

    [Fact]
    public async Task Leaderboard_GetPlayerRank_ReturnsOk()
    {
        var (client, auth) = await CreateAuthenticatedClient();

        var response = await client.GetAsync($"/api/v1/leaderboard/player/{auth.PlayerId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Leaderboard_GetPlayerRank_NonExistent_ReturnsNotFound()
    {
        var (client, _) = await CreateAuthenticatedClient();

        var response = await client.GetAsync($"/api/v1/leaderboard/player/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ── Tournaments ──

    [Fact]
    public async Task Tournament_GetCurrent_ReturnsOk()
    {
        var (client, _) = await CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/v1/tournament/current");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Tournament_Enter_NoTeam_ReturnsBadRequest()
    {
        var (client, _) = await CreateAuthenticatedClient();

        var request = new { teamId = Guid.NewGuid() };
        var response = await client.PostAsJsonAsync("/api/v1/tournament/enter", request);

        Assert.True(
            response.StatusCode == HttpStatusCode.BadRequest ||
            response.StatusCode == HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Tournament_GetBracket_NonExistent_ReturnsNotFound()
    {
        var (client, _) = await CreateAuthenticatedClient();

        var response = await client.GetAsync($"/api/v1/tournament/bracket/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Tournament_Unauthenticated_ReturnsUnauthorized()
    {
        var client = CreateClient();

        var response = await client.GetAsync("/api/v1/tournament/current");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── Guild Wars ──

    [Fact]
    public async Task GuildWar_GetStatus_ReturnsOk()
    {
        var (client, _) = await CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/v1/guildwar/status");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(content, JsonOptions);
        Assert.False(json.GetProperty("isAtWar").GetBoolean());
    }

    [Fact]
    public async Task GuildWar_GetHistory_ReturnsOkEmptyArray()
    {
        var (client, _) = await CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/v1/guildwar/history");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(content, JsonOptions);
        Assert.Equal(JsonValueKind.Array, json.ValueKind);
        Assert.Equal(0, json.GetArrayLength());
    }

    [Fact]
    public async Task GuildWar_Unauthenticated_ReturnsUnauthorized()
    {
        var client = CreateClient();

        var response = await client.GetAsync("/api/v1/guildwar/status");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
