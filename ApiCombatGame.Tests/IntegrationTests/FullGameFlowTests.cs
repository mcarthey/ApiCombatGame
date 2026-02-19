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

public class FullGameFlowTests : IntegrationTestBase
{
    public FullGameFlowTests(WebApplicationFactory<Program> factory) : base(factory, "FullFlow")
    {
    }

    [Fact]
    public async Task FullGameFlow_TwoPlayers_RegisterBuildTeamBattleAndVerify()
    {
        // Step 1: Register Player A, get token, set auth header
        var (clientA, authA) = await CreateAuthenticatedClient();

        // Step 2: GET /api/v1/player/profile — assert OK, verify level=1, currency=1000
        var profileResponse = await clientA.GetAsync("/api/v1/player/profile");
        Assert.Equal(HttpStatusCode.OK, profileResponse.StatusCode);

        var profileContent = await profileResponse.Content.ReadAsStringAsync();
        var profile = JsonDocument.Parse(profileContent).RootElement;
        Assert.Equal(1, profile.GetProperty("level").GetInt32());
        Assert.Equal(1000, profile.GetProperty("currency").GetInt32());

        // Step 3: GET /api/v1/player/roster — assert OK, extract unit IDs (take first 3)
        var rosterResponseA = await clientA.GetAsync("/api/v1/player/roster");
        Assert.Equal(HttpStatusCode.OK, rosterResponseA.StatusCode);

        var rosterContentA = await rosterResponseA.Content.ReadAsStringAsync();
        var rosterA = JsonSerializer.Deserialize<List<JsonElement>>(rosterContentA, JsonOptions)!;
        Assert.True(rosterA.Count >= 3, "Player A should have at least 3 starter units.");
        var unitIdsA = rosterA.Select(u => u.GetProperty("id").GetGuid()).Take(3).ToList();

        // Step 4: POST /api/v1/team/configure — assert Created
        var teamRequestA = new TeamConfigRequest
        {
            Name = "Alpha Team",
            UnitIds = unitIdsA,
            Strategy = new StrategyConfig
            {
                Formation = Formation.aggressive,
                TargetPriority = new List<TargetPriority> { TargetPriority.lowest_hp }
            }
        };

        var teamResponseA = await clientA.PostAsJsonAsync("/api/v1/team/configure", teamRequestA);
        Assert.Equal(HttpStatusCode.Created, teamResponseA.StatusCode);

        var teamContentA = await teamResponseA.Content.ReadAsStringAsync();
        var teamA = JsonSerializer.Deserialize<TeamResponse>(teamContentA, JsonOptions)!;
        Assert.Equal("Alpha Team", teamA.Name);

        // Step 5: Register Player B with new client, get token
        var (clientB, authB) = await CreateAuthenticatedClient();

        // Step 6: GET /api/v1/player/roster for B — extract unit IDs
        var rosterResponseB = await clientB.GetAsync("/api/v1/player/roster");
        Assert.Equal(HttpStatusCode.OK, rosterResponseB.StatusCode);

        var rosterContentB = await rosterResponseB.Content.ReadAsStringAsync();
        var rosterB = JsonSerializer.Deserialize<List<JsonElement>>(rosterContentB, JsonOptions)!;
        Assert.True(rosterB.Count >= 3, "Player B should have at least 3 starter units.");
        var unitIdsB = rosterB.Select(u => u.GetProperty("id").GetGuid()).Take(3).ToList();

        // Step 7: POST /api/v1/team/configure for B — assert Created
        var teamRequestB = new TeamConfigRequest
        {
            Name = "Bravo Team",
            UnitIds = unitIdsB,
            Strategy = new StrategyConfig
            {
                Formation = Formation.defensive,
                TargetPriority = new List<TargetPriority> { TargetPriority.highest_threat }
            }
        };

        var teamResponseB = await clientB.PostAsJsonAsync("/api/v1/team/configure", teamRequestB);
        Assert.Equal(HttpStatusCode.Created, teamResponseB.StatusCode);

        var teamContentB = await teamResponseB.Content.ReadAsStringAsync();
        var teamB = JsonSerializer.Deserialize<TeamResponse>(teamContentB, JsonOptions)!;
        Assert.Equal("Bravo Team", teamB.Name);

        // Step 8: POST /api/v1/battle/queue for A — assert Created, verify status="queued"
        var queueRequestA = new BattleQueueRequest
        {
            TeamId = teamA.Id,
            Mode = "ranked"
        };

        var queueResponseA = await clientA.PostAsJsonAsync("/api/v1/battle/queue", queueRequestA);
        Assert.Equal(HttpStatusCode.Created, queueResponseA.StatusCode);

        var queueContentA = await queueResponseA.Content.ReadAsStringAsync();
        var battleStatusA = JsonSerializer.Deserialize<BattleStatusResponse>(queueContentA, JsonOptions)!;
        Assert.Equal("queued", battleStatusA.Status);
        Assert.NotEqual(Guid.Empty, battleStatusA.BattleId);

        // Step 9: POST /api/v1/battle/queue for B — assert Created
        var queueRequestB = new BattleQueueRequest
        {
            TeamId = teamB.Id,
            Mode = "ranked"
        };

        var queueResponseB = await clientB.PostAsJsonAsync("/api/v1/battle/queue", queueRequestB);
        Assert.Equal(HttpStatusCode.Created, queueResponseB.StatusCode);

        // Step 10: GET /api/v1/battle/status/{battleId} for A — assert OK
        var statusResponse = await clientA.GetAsync($"/api/v1/battle/status/{battleStatusA.BattleId}");
        Assert.Equal(HttpStatusCode.OK, statusResponse.StatusCode);

        // Step 11: GET /api/v1/leaderboard — assert OK, verify at least 2 entries
        var leaderboardResponse = await clientA.GetAsync("/api/v1/leaderboard");
        Assert.Equal(HttpStatusCode.OK, leaderboardResponse.StatusCode);

        var leaderboardContent = await leaderboardResponse.Content.ReadAsStringAsync();
        var leaderboard = JsonSerializer.Deserialize<List<JsonElement>>(leaderboardContent, JsonOptions)!;
        Assert.True(leaderboard.Count >= 2, "Leaderboard should contain at least 2 entries after registering two players.");
    }

    [Fact]
    public async Task FullGameFlow_PlayerProfileUpdatesAfterRegistration()
    {
        // Step 1: Register and get profile
        var username = $"profile_test_{Guid.NewGuid():N}";
        var client = CreateClient();
        var auth = await RegisterPlayer(client, username);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);

        var profileResponse = await client.GetAsync("/api/v1/player/profile");
        Assert.Equal(HttpStatusCode.OK, profileResponse.StatusCode);

        // Step 2: Login with same credentials (email-based)
        var loginRequest = new LoginRequest
        {
            Email = $"{username}@test.com",
            Password = "SecurePass123!"
        };

        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        loginResponse.EnsureSuccessStatusCode();

        var loginContent = await loginResponse.Content.ReadAsStringAsync();
        var loginAuth = JsonSerializer.Deserialize<AuthResponse>(loginContent, JsonOptions)!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginAuth.Token);

        // Step 3: Get profile again — verify player exists, has basic fields
        var profileResponse2 = await client.GetAsync("/api/v1/player/profile");
        Assert.Equal(HttpStatusCode.OK, profileResponse2.StatusCode);

        var profileContent2 = await profileResponse2.Content.ReadAsStringAsync();
        var profile2 = JsonDocument.Parse(profileContent2).RootElement;

        Assert.True(profile2.TryGetProperty("id", out var idProp));
        Assert.NotEqual(Guid.Empty, idProp.GetGuid());

        Assert.True(profile2.TryGetProperty("username", out var usernameProp));
        Assert.False(string.IsNullOrEmpty(usernameProp.GetString()));

        Assert.True(profile2.TryGetProperty("level", out var levelProp));
        Assert.True(levelProp.GetInt32() >= 1);

        Assert.True(profile2.TryGetProperty("currency", out var currencyProp));
        Assert.True(currencyProp.GetInt32() >= 0);
    }

    [Fact]
    public async Task FullGameFlow_LoginStreak_NewPlayerHasStreakFields()
    {
        // Step 1: Register and authenticate
        var (client, auth) = await CreateAuthenticatedClient();

        // Step 2: GET /api/v1/player/profile
        var profileResponse = await client.GetAsync("/api/v1/player/profile");
        Assert.Equal(HttpStatusCode.OK, profileResponse.StatusCode);

        var profileContent = await profileResponse.Content.ReadAsStringAsync();
        var profile = JsonDocument.Parse(profileContent).RootElement;

        // Step 3: Verify response has winStreak (int, >= 0) and lastLoginAt (exists)
        Assert.True(profile.TryGetProperty("winStreak", out var winStreak),
            "Profile should contain a 'winStreak' field.");
        Assert.Equal(JsonValueKind.Number, winStreak.ValueKind);
        Assert.True(winStreak.GetInt32() >= 0, "Win streak should be >= 0 for a new player.");

        Assert.True(profile.TryGetProperty("lastLoginAt", out var lastLoginAt),
            "Profile should contain a 'lastLoginAt' field.");
        // lastLoginAt should be a non-null value (string date or similar)
        Assert.NotEqual(JsonValueKind.Undefined, lastLoginAt.ValueKind);
    }

    [Fact]
    public async Task FullGameFlow_MultipleTeams_CreateAndList()
    {
        // Step 1: Register and authenticate
        var (client, auth) = await CreateAuthenticatedClient();

        // Step 2: Get roster, take unit IDs
        var rosterResponse = await client.GetAsync("/api/v1/player/roster");
        Assert.Equal(HttpStatusCode.OK, rosterResponse.StatusCode);

        var rosterContent = await rosterResponse.Content.ReadAsStringAsync();
        var roster = JsonSerializer.Deserialize<List<JsonElement>>(rosterContent, JsonOptions)!;
        Assert.True(roster.Count >= 1, "Player should have at least 1 starter unit.");
        var unitIds = roster.Select(u => u.GetProperty("id").GetGuid()).Take(3).ToList();

        // Step 3: Create team "Team Alpha" — assert Created
        var teamAlphaRequest = new TeamConfigRequest
        {
            Name = "Team Alpha",
            UnitIds = unitIds,
            Strategy = new StrategyConfig
            {
                Formation = Formation.aggressive,
                TargetPriority = new List<TargetPriority> { TargetPriority.lowest_hp }
            }
        };

        var teamAlphaResponse = await client.PostAsJsonAsync("/api/v1/team/configure", teamAlphaRequest);
        Assert.Equal(HttpStatusCode.Created, teamAlphaResponse.StatusCode);

        // Step 4: Create team "Team Beta" — assert Created
        var teamBetaRequest = new TeamConfigRequest
        {
            Name = "Team Beta",
            UnitIds = unitIds,
            Strategy = new StrategyConfig
            {
                Formation = Formation.defensive,
                TargetPriority = new List<TargetPriority> { TargetPriority.highest_threat, TargetPriority.healers }
            }
        };

        var teamBetaResponse = await client.PostAsJsonAsync("/api/v1/team/configure", teamBetaRequest);
        Assert.Equal(HttpStatusCode.Created, teamBetaResponse.StatusCode);

        // Step 5: GET /api/v1/team/list — assert OK, verify at least 2 teams
        var listResponse = await client.GetAsync("/api/v1/team/list");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        var listContent = await listResponse.Content.ReadAsStringAsync();
        var teams = JsonSerializer.Deserialize<List<JsonElement>>(listContent, JsonOptions)!;
        Assert.True(teams.Count >= 2, "Team list should contain at least 2 teams after creating Team Alpha and Team Beta.");
    }
}
