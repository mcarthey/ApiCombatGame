using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ApiCombatGame.Data;
using ApiCombatGame.Models.DTOs.Auth;
using ApiCombatGame.Models.DTOs.Battle;
using ApiCombatGame.Models.DTOs.Strategy;
using ApiCombatGame.Models.DTOs.Team;
using ApiCombatGame.Tests.Helpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ApiCombatGame.Tests.IntegrationTests;

public class BattleFlowTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public BattleFlowTests(WebApplicationFactory<Program> factory)
    {
        IntegrationTestSetup.DisableRateLimiting();
        var dbName = $"TestDb_Battle_{Guid.NewGuid()}";
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<GameDbContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                services.AddDbContext<GameDbContext>(options =>
                    options.UseInMemoryDatabase(dbName)
                        .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning)));
            });
        });
    }

    [Fact]
    public async Task FullBattleFlow_Register_Login_CreateTeam_QueueBattle()
    {
        var client = _factory.CreateClient();

        // Step 1: Register player
        var registerRequest = new RegisterRequest
        {
            Username = $"warrior_{Guid.NewGuid():N}",
            Email = $"warrior_{Guid.NewGuid():N}@example.com",
            Password = "SecurePass123!"
        };

        var registerResponse = await client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        var authContent = await registerResponse.Content.ReadAsStringAsync();
        var auth = JsonSerializer.Deserialize<AuthResponse>(authContent, JsonOptions)!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);

        // Step 2: Get roster
        var rosterResponse = await client.GetAsync("/api/v1/player/roster");
        Assert.Equal(HttpStatusCode.OK, rosterResponse.StatusCode);

        var rosterContent = await rosterResponse.Content.ReadAsStringAsync();
        var roster = JsonSerializer.Deserialize<List<JsonElement>>(rosterContent, JsonOptions)!;
        Assert.True(roster.Count >= 1, "Player should have starter units.");

        // Step 3: Create team with owned units
        var unitIds = roster.Select(u => u.GetProperty("id").GetGuid()).Take(3).ToList();

        var teamRequest = new TeamConfigRequest
        {
            Name = "Test Team",
            UnitIds = unitIds,
            Strategy = new StrategyConfig
            {
                Formation = Formation.aggressive,
                TargetPriority = new List<TargetPriority> { TargetPriority.lowest_hp }
            }
        };

        var teamResponse = await client.PostAsJsonAsync("/api/v1/team/configure", teamRequest);
        var teamContent = await teamResponse.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.Created, teamResponse.StatusCode);

        var team = JsonSerializer.Deserialize<TeamResponse>(teamContent, JsonOptions)!;
        Assert.Equal("Test Team", team.Name);

        // Step 4: Queue battle
        var queueRequest = new BattleQueueRequest
        {
            TeamId = team.Id,
            Mode = "ranked"
        };

        var queueResponse = await client.PostAsJsonAsync("/api/v1/battle/queue", queueRequest);
        var queueContent = await queueResponse.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.Created, queueResponse.StatusCode);

        var battleStatus = JsonSerializer.Deserialize<BattleStatusResponse>(queueContent, JsonOptions)!;
        Assert.Equal("queued", battleStatus.Status);
        Assert.NotEqual(Guid.Empty, battleStatus.BattleId);

        // Step 5: Check battle status
        var statusResponse = await client.GetAsync($"/api/v1/battle/status/{battleStatus.BattleId}");
        Assert.Equal(HttpStatusCode.OK, statusResponse.StatusCode);
    }

    [Fact]
    public async Task CreateTeam_WithTooManyUnits_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var auth = await RegisterAndGetAuth(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);

        // Try to create a team with 6 fake unit IDs (more than max 5)
        var teamRequest = new TeamConfigRequest
        {
            Name = "Too Big Team",
            UnitIds = Enumerable.Range(0, 6).Select(_ => Guid.NewGuid()).ToList()
        };

        var response = await client.PostAsJsonAsync("/api/v1/team/configure", teamRequest);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task QueueBattle_WithInvalidTeam_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var auth = await RegisterAndGetAuth(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);

        var queueRequest = new BattleQueueRequest
        {
            TeamId = Guid.NewGuid(), // Non-existent team
            Mode = "ranked"
        };

        var response = await client.PostAsJsonAsync("/api/v1/battle/queue", queueRequest);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetBattleStatus_NonExistentBattle_ReturnsNotFound()
    {
        var client = _factory.CreateClient();

        var auth = await RegisterAndGetAuth(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);

        var response = await client.GetAsync($"/api/v1/battle/status/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetLeaderboard_ReturnsPlayerList()
    {
        var client = _factory.CreateClient();

        var auth = await RegisterAndGetAuth(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);

        var response = await client.GetAsync("/api/v1/leaderboard");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var leaderboard = JsonSerializer.Deserialize<List<JsonElement>>(content, JsonOptions);
        Assert.NotNull(leaderboard);
        Assert.True(leaderboard!.Count >= 1);
    }

    [Fact]
    public async Task TeamCRUD_CreateReadUpdateDelete()
    {
        var client = _factory.CreateClient();

        var auth = await RegisterAndGetAuth(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);

        // Get roster for unit IDs
        var rosterResponse = await client.GetAsync("/api/v1/player/roster");
        var rosterContent = await rosterResponse.Content.ReadAsStringAsync();
        var roster = JsonSerializer.Deserialize<List<JsonElement>>(rosterContent, JsonOptions)!;
        var unitIds = roster.Select(u => u.GetProperty("id").GetGuid()).Take(2).ToList();

        // Create
        var createRequest = new TeamConfigRequest
        {
            Name = "CRUD Team",
            UnitIds = unitIds
        };
        var createResponse = await client.PostAsJsonAsync("/api/v1/team/configure", createRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var team = JsonSerializer.Deserialize<TeamResponse>(await createResponse.Content.ReadAsStringAsync(), JsonOptions)!;

        // Read
        var readResponse = await client.GetAsync($"/api/v1/team/{team.Id}");
        Assert.Equal(HttpStatusCode.OK, readResponse.StatusCode);

        // List
        var listResponse = await client.GetAsync("/api/v1/team/list");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        // Update
        var updateRequest = new TeamConfigRequest
        {
            Name = "Updated CRUD Team",
            UnitIds = unitIds
        };
        var updateResponse = await client.PutAsJsonAsync($"/api/v1/team/{team.Id}", updateRequest);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        // Delete
        var deleteResponse = await client.DeleteAsync($"/api/v1/team/{team.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // Verify deleted
        var verifyResponse = await client.GetAsync($"/api/v1/team/{team.Id}");
        Assert.Equal(HttpStatusCode.NotFound, verifyResponse.StatusCode);
    }

    private async Task<AuthResponse> RegisterAndGetAuth(HttpClient client)
    {
        var request = new RegisterRequest
        {
            Username = $"testplayer_{Guid.NewGuid():N}",
            Email = $"test_{Guid.NewGuid():N}@example.com",
            Password = "SecurePass123!"
        };

        var response = await client.PostAsJsonAsync("/api/v1/auth/register", request);
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<AuthResponse>(content, JsonOptions)!;
    }
}
