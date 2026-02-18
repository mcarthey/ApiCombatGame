using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ApiCombatGame.Models.DTOs.Strategy;
using ApiCombatGame.Models.DTOs.Team;
using ApiCombatGame.Tests.Helpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ApiCombatGame.Tests.IntegrationTests;

public class TeamApiTests : IntegrationTestBase
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public TeamApiTests(WebApplicationFactory<Program> factory) : base(factory, "TeamApi") { }

    private async Task<List<Guid>> GetRosterUnitIds(HttpClient client, int take = 3)
    {
        var response = await client.GetAsync("/api/v1/player/roster");
        var content = await response.Content.ReadAsStringAsync();
        var roster = JsonSerializer.Deserialize<List<JsonElement>>(content, Json)!;
        return roster.Select(u => u.GetProperty("id").GetGuid()).Take(take).ToList();
    }

    [Fact]
    public async Task ConfigureTeam_ValidUnits_Returns201()
    {
        var (client, _) = await CreateAuthenticatedClient();
        var unitIds = await GetRosterUnitIds(client);

        var response = await client.PostAsJsonAsync("/api/v1/team/configure", new TeamConfigRequest
        {
            Name = "My Team",
            UnitIds = unitIds,
            Strategy = new StrategyConfig { Formation = "aggressive", TargetPriority = new List<string> { "healers", "lowest_hp" } }
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var team = JsonSerializer.Deserialize<TeamResponse>(content, Json)!;
        Assert.Equal("My Team", team.Name);
        Assert.Equal(unitIds.Count, team.Units.Count);
    }

    [Fact]
    public async Task ConfigureTeam_UnownedUnit_Returns400()
    {
        var (client, _) = await CreateAuthenticatedClient();

        var response = await client.PostAsJsonAsync("/api/v1/team/configure", new TeamConfigRequest
        {
            Name = "Bad Team",
            UnitIds = new List<Guid> { Guid.NewGuid() } // Non-existent unit
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("not found in your roster", content);
    }

    [Fact]
    public async Task ConfigureTeam_EmptyUnits_Returns400()
    {
        var (client, _) = await CreateAuthenticatedClient();

        var response = await client.PostAsJsonAsync("/api/v1/team/configure", new TeamConfigRequest
        {
            Name = "Empty Team",
            UnitIds = new List<Guid>()
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ConfigureTeam_Over5Units_Returns400()
    {
        var (client, _) = await CreateAuthenticatedClient();

        var response = await client.PostAsJsonAsync("/api/v1/team/configure", new TeamConfigRequest
        {
            Name = "Too Big Team",
            UnitIds = Enumerable.Range(0, 6).Select(_ => Guid.NewGuid()).ToList()
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetTeam_ReturnsUnitsAndStrategy()
    {
        var (client, _) = await CreateAuthenticatedClient();
        var unitIds = await GetRosterUnitIds(client);

        var createResponse = await client.PostAsJsonAsync("/api/v1/team/configure", new TeamConfigRequest
        {
            Name = "Detail Team",
            UnitIds = unitIds,
            Strategy = new StrategyConfig { Formation = "defensive" }
        });
        var created = JsonSerializer.Deserialize<TeamResponse>(
            await createResponse.Content.ReadAsStringAsync(), Json)!;

        var response = await client.GetAsync($"/api/v1/team/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var team = JsonSerializer.Deserialize<TeamResponse>(
            await response.Content.ReadAsStringAsync(), Json)!;
        Assert.Equal("Detail Team", team.Name);
        Assert.Equal(unitIds.Count, team.Units.Count);
        Assert.NotNull(team.Strategy);
        Assert.Equal("defensive", team.Strategy!.Formation);
    }

    [Fact]
    public async Task GetTeam_OtherPlayersTeam_Returns404()
    {
        var (client1, _) = await CreateAuthenticatedClient($"teamowner_{Guid.NewGuid():N}");
        var unitIds = await GetRosterUnitIds(client1);

        var createResponse = await client1.PostAsJsonAsync("/api/v1/team/configure", new TeamConfigRequest
        {
            Name = "Private Team",
            UnitIds = unitIds
        });
        var created = JsonSerializer.Deserialize<TeamResponse>(
            await createResponse.Content.ReadAsStringAsync(), Json)!;

        // Other player tries to access
        var (client2, _) = await CreateAuthenticatedClient($"teamsnoop_{Guid.NewGuid():N}");
        var response = await client2.GetAsync($"/api/v1/team/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ListTeams_ReturnsAllPlayerTeams()
    {
        var (client, _) = await CreateAuthenticatedClient();
        var unitIds = await GetRosterUnitIds(client);

        // Create 2 teams
        await client.PostAsJsonAsync("/api/v1/team/configure", new TeamConfigRequest { Name = "Team A", UnitIds = unitIds });
        await client.PostAsJsonAsync("/api/v1/team/configure", new TeamConfigRequest { Name = "Team B", UnitIds = unitIds.Take(1).ToList() });

        var response = await client.GetAsync("/api/v1/team/list");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var teams = JsonSerializer.Deserialize<List<TeamResponse>>(
            await response.Content.ReadAsStringAsync(), Json)!;
        Assert.Equal(2, teams.Count);
    }

    [Fact]
    public async Task UpdateTeam_ChangesUnitsAndStrategy()
    {
        var (client, _) = await CreateAuthenticatedClient();
        var unitIds = await GetRosterUnitIds(client);

        var createResponse = await client.PostAsJsonAsync("/api/v1/team/configure", new TeamConfigRequest
        {
            Name = "Original",
            UnitIds = unitIds,
            Strategy = new StrategyConfig { Formation = "balanced" }
        });
        var created = JsonSerializer.Deserialize<TeamResponse>(
            await createResponse.Content.ReadAsStringAsync(), Json)!;

        // Update with fewer units and different strategy
        var response = await client.PutAsJsonAsync($"/api/v1/team/{created.Id}", new TeamConfigRequest
        {
            Name = "Updated",
            UnitIds = unitIds.Take(1).ToList(),
            Strategy = new StrategyConfig { Formation = "aggressive" }
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var updated = JsonSerializer.Deserialize<TeamResponse>(
            await response.Content.ReadAsStringAsync(), Json)!;
        Assert.Equal("Updated", updated.Name);
        Assert.Single(updated.Units);
        Assert.Equal("aggressive", updated.Strategy?.Formation);
    }

    [Fact]
    public async Task DeleteTeam_RemovesTeam_KeepsUnits()
    {
        var (client, _) = await CreateAuthenticatedClient();
        var unitIds = await GetRosterUnitIds(client);

        var createResponse = await client.PostAsJsonAsync("/api/v1/team/configure", new TeamConfigRequest
        {
            Name = "Delete Me",
            UnitIds = unitIds
        });
        var created = JsonSerializer.Deserialize<TeamResponse>(
            await createResponse.Content.ReadAsStringAsync(), Json)!;

        // Delete
        var deleteResponse = await client.DeleteAsync($"/api/v1/team/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // Verify team is gone
        var getResponse = await client.GetAsync($"/api/v1/team/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);

        // Verify units still in roster
        var rosterResponse = await client.GetAsync("/api/v1/player/roster");
        var roster = JsonSerializer.Deserialize<List<JsonElement>>(
            await rosterResponse.Content.ReadAsStringAsync(), Json)!;
        Assert.Equal(3, roster.Count); // Starter units still there
    }
}
