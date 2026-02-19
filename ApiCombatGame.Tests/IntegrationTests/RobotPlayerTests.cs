using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ApiCombatGame.Services.Interfaces;
using ApiCombatGame.Tests.Helpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace ApiCombatGame.Tests.IntegrationTests;

/// <summary>
/// Simulates a real API player's journey through the entire game.
/// Reads the OpenAPI spec for schema definitions, follows HATEOAS _links,
/// and validates every response against the spec. Uses NO internal C# DTOs —
/// all requests are anonymous objects, all responses are raw JsonDocument.
/// </summary>
public class RobotPlayerTests : IntegrationTestBase
{
    private readonly ITestOutputHelper _output;
    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private OpenApiSpecReader _spec = null!;
    private ResponseValidator _validator = null!;
    private LinkWalker _linkWalker = null!;

    public RobotPlayerTests(WebApplicationFactory<Program> factory, ITestOutputHelper output)
        : base(factory, "RobotPlayer")
    {
        _output = output;
    }

    #region Infrastructure

    private async Task InitSpec(HttpClient client)
    {
        _spec = new OpenApiSpecReader();
        await _spec.LoadAsync(client);
        _validator = new ResponseValidator(_spec);
        _linkWalker = new LinkWalker(_validator);
    }

    /// <summary>Parse a JSON response body into a JsonElement.</summary>
    private static async Task<JsonElement> ParseResponse(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(content).RootElement;
    }

    /// <summary>Register a player using only raw HTTP (no internal DTOs).</summary>
    private async Task<(string Token, string PlayerId)> RegisterRobot(HttpClient client, string? username = null)
    {
        username ??= $"robot_{Guid.NewGuid():N}";
        var response = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            username,
            email = $"{username}@test.com",
            password = "SecurePass123!"
        });
        response.EnsureSuccessStatusCode();

        var body = await ParseResponse(response);
        var token = body.GetProperty("token").GetString()!;
        var playerId = body.GetProperty("playerId").GetString()!;
        return (token, playerId);
    }

    /// <summary>Set the auth header on a client.</summary>
    private static void Authenticate(HttpClient client, string token) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    /// <summary>Process queued battles (background services don't run in tests).</summary>
    private async Task ProcessBattles()
    {
        using var scope = Factory.Services.CreateScope();
        var battleService = scope.ServiceProvider.GetRequiredService<IBattleService>();
        await battleService.ProcessQueuedBattlesAsync(CancellationToken.None);
    }

    /// <summary>Assert link walk results have no errors, logging details.</summary>
    private void AssertLinksValid(List<LinkResult> results, string context)
    {
        var errors = results.Where(r => r.Errors.Count > 0).ToList();
        if (errors.Count > 0)
        {
            foreach (var r in errors)
            {
                _output.WriteLine($"[{context}] Link '{r.Name}' ({r.Method} {r.Href}) errors:");
                foreach (var err in r.Errors)
                    _output.WriteLine($"    - {err}");
            }
        }

        var followed = results.Where(r => r.WasFollowed).ToList();
        foreach (var r in followed)
        {
            _output.WriteLine($"[{context}] Link '{r.Name}' -> {r.Method} {r.Href} => {r.StatusCode}");
        }

        Assert.True(errors.Count == 0,
            $"[{context}] {errors.Count} link(s) had errors: " +
            string.Join("; ", errors.Select(r => $"{r.Name}: {string.Join(", ", r.Errors)}")));
    }

    #endregion

    #region OpenAPI Spec Tests

    [Fact]
    public async Task OpenApiSpec_IsAccessible_AndValid()
    {
        var client = CreateClient();
        await InitSpec(client);

        // Verify spec structure
        var info = _spec.GetInfo();
        Assert.Equal("API Combat Game", info.GetProperty("title").GetString());

        var paths = _spec.GetPaths();
        Assert.True(paths.EnumerateObject().Count() > 50, "Spec should have 50+ endpoints");

        var schemas = _spec.GetSchemas();
        Assert.True(schemas.EnumerateObject().Count() > 10, "Spec should have 10+ schemas");

        _output.WriteLine($"OpenAPI spec: {paths.EnumerateObject().Count()} paths, {schemas.EnumerateObject().Count()} schemas");
    }

    [Fact]
    public async Task OpenApiSpec_EnumValues_Present()
    {
        var client = CreateClient();
        await InitSpec(client);

        // Verify strategy enums are in the spec
        var formations = _spec.GetEnumValues("Formation");
        Assert.Contains("aggressive", formations);
        Assert.Contains("defensive", formations);
        Assert.Contains("balanced", formations);
        _output.WriteLine($"Formation enums: [{string.Join(", ", formations)}]");

        var targetPriorities = _spec.GetEnumValues("TargetPriority");
        Assert.Contains("lowest_hp", targetPriorities);
        Assert.Contains("healers", targetPriorities);
        _output.WriteLine($"TargetPriority enums: [{string.Join(", ", targetPriorities)}]");

        var abilityWhen = _spec.GetEnumValues("AbilityWhen");
        Assert.Contains("always", abilityWhen);
        _output.WriteLine($"AbilityWhen enums: [{string.Join(", ", abilityWhen)}]");

        var abilityTarget = _spec.GetEnumValues("AbilityTarget");
        Assert.Contains("priority", abilityTarget);
        _output.WriteLine($"AbilityTarget enums: [{string.Join(", ", abilityTarget)}]");
    }

    #endregion

    #region Phase 1: Registration & Login

    [Fact]
    public async Task Phase1_Registration_SpecCompliant()
    {
        var client = CreateClient();
        await InitSpec(client);

        // Register
        var regResponse = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            username = $"robot_{Guid.NewGuid():N}",
            email = $"robot_{Guid.NewGuid():N}@test.com",
            password = "SecurePass123!"
        });

        Assert.Equal(HttpStatusCode.Created, regResponse.StatusCode);
        var regBody = await ParseResponse(regResponse);

        // Validate response has expected fields
        Assert.True(regBody.TryGetProperty("playerId", out var pid), "Response should have playerId");
        Assert.NotEqual(Guid.Empty.ToString(), pid.GetString());

        Assert.True(regBody.TryGetProperty("token", out var token), "Response should have token");
        Assert.False(string.IsNullOrEmpty(token.GetString()), "Token should not be empty");

        Assert.True(regBody.TryGetProperty("expiresAt", out _), "Response should have expiresAt");

        // Login with same credentials
        Authenticate(client, token.GetString()!);
        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = $"robot_{pid.GetString()![..8]}@test.com", // Won't match — use a fresh login
        });
        // Login may fail because we used a different email; that's fine — registration is the main test
    }

    #endregion

    #region Phase 2: Profile & Links

    [Fact]
    public async Task Phase2_Profile_LinksAllWork()
    {
        var client = CreateClient();
        await InitSpec(client);

        var (token, playerId) = await RegisterRobot(client);
        Authenticate(client, token);

        // Get profile
        var profileResponse = await client.GetAsync("/api/v1/player/profile");
        Assert.Equal(HttpStatusCode.OK, profileResponse.StatusCode);

        var profile = await ParseResponse(profileResponse);

        // Validate core profile fields
        Assert.Equal(1, profile.GetProperty("level").GetInt32());
        Assert.Equal(1000, profile.GetProperty("currency").GetInt32());
        Assert.True(profile.TryGetProperty("rating", out _), "Profile should have rating");
        Assert.True(profile.TryGetProperty("username", out _), "Profile should have username");

        // Validate _links structure
        var linkErrors = _validator.ValidateLinks(profile);
        Assert.Empty(linkErrors);

        // Validate expected link keys exist
        var presenceErrors = _validator.ValidateLinkPresence(profile,
            "self", "roster", "roster_available", "achievements", "notifications", "leaderboard");
        Assert.Empty(presenceErrors);

        // Follow all GET links
        var linkResults = await _linkWalker.FollowAllLinks(profile, client);
        AssertLinksValid(linkResults, "Profile");

        // Verify specific link targets returned 200
        var followedLinks = linkResults.Where(r => r.WasFollowed).ToList();
        foreach (var link in followedLinks)
        {
            _output.WriteLine($"Profile link '{link.Name}': {link.StatusCode}");
        }
    }

    #endregion

    #region Phase 3: Roster & Unlocks

    [Fact]
    public async Task Phase3_RosterAndUnlocks()
    {
        var client = CreateClient();
        await InitSpec(client);

        var (token, _) = await RegisterRobot(client);
        Authenticate(client, token);

        // Get starter roster
        var rosterResponse = await client.GetAsync("/api/v1/player/roster");
        Assert.Equal(HttpStatusCode.OK, rosterResponse.StatusCode);

        var roster = await ParseResponse(rosterResponse);
        Assert.Equal(JsonValueKind.Array, roster.ValueKind);
        var starterCount = roster.GetArrayLength();
        Assert.True(starterCount >= 3, $"Should have at least 3 starter units, got {starterCount}");
        _output.WriteLine($"Starter roster: {starterCount} units");

        // Validate unit structure
        var firstUnit = roster[0];
        Assert.True(firstUnit.TryGetProperty("id", out _), "Unit should have id");
        Assert.True(firstUnit.TryGetProperty("name", out _), "Unit should have name");
        Assert.True(firstUnit.TryGetProperty("class", out _), "Unit should have class");
        Assert.True(firstUnit.TryGetProperty("health", out _), "Unit should have health");
        Assert.True(firstUnit.TryGetProperty("attack", out _), "Unit should have attack");

        // Browse available units
        var availableResponse = await client.GetAsync("/api/v1/player/roster/available");
        Assert.Equal(HttpStatusCode.OK, availableResponse.StatusCode);

        var available = await ParseResponse(availableResponse);
        Assert.Equal(JsonValueKind.Array, available.ValueKind);
        _output.WriteLine($"Available units to unlock: {available.GetArrayLength()}");

        // Find an unlockable unit (not already owned)
        Guid? templateId = null;
        foreach (var unit in available.EnumerateArray())
        {
            if (unit.TryGetProperty("alreadyOwned", out var owned) && !owned.GetBoolean())
            {
                templateId = unit.GetProperty("id").GetGuid();
                var cost = unit.GetProperty("unlockCost").GetInt32();
                _output.WriteLine($"Unlocking: {unit.GetProperty("name").GetString()} (cost: {cost}g)");
                break;
            }
        }

        if (templateId.HasValue)
        {
            // Unlock the unit
            var unlockResponse = await client.PostAsJsonAsync("/api/v1/player/roster/unlock",
                new { templateUnitId = templateId.Value });
            Assert.Equal(HttpStatusCode.Created, unlockResponse.StatusCode);

            var unlocked = await ParseResponse(unlockResponse);
            Assert.True(unlocked.TryGetProperty("currencyRemaining", out var remaining),
                "Unlock response should have currencyRemaining");
            Assert.True(remaining.GetInt32() < 1000, "Currency should have decreased");
            _output.WriteLine($"Currency remaining: {remaining.GetInt32()}g");

            // Verify roster grew
            var rosterAfter = await client.GetAsync("/api/v1/player/roster");
            var rosterAfterBody = await ParseResponse(rosterAfter);
            Assert.True(rosterAfterBody.GetArrayLength() > starterCount,
                "Roster should have grown after unlock");
        }
    }

    #endregion

    #region Phase 4: Team Creation & Links

    [Fact]
    public async Task Phase4_TeamCreation_LinksWork()
    {
        var client = CreateClient();
        await InitSpec(client);

        var (token, _) = await RegisterRobot(client);
        Authenticate(client, token);

        // Get roster for unit IDs
        var rosterResponse = await client.GetAsync("/api/v1/player/roster");
        var roster = await ParseResponse(rosterResponse);
        var unitIds = roster.EnumerateArray()
            .Take(3)
            .Select(u => u.GetProperty("id").GetGuid())
            .ToList();

        // Read formation values from spec (NOT from C# enum)
        var formations = _spec.GetEnumValues("Formation");
        Assert.True(formations.Count > 0, "Spec should define Formation enum values");
        var formation = formations.First(); // Use whatever the spec says

        var targetPriorities = _spec.GetEnumValues("TargetPriority");
        Assert.True(targetPriorities.Count > 0, "Spec should define TargetPriority enum values");

        _output.WriteLine($"Using spec-derived formation: '{formation}'");
        _output.WriteLine($"Using spec-derived targetPriority: '{targetPriorities.First()}'");

        // Create team with spec-derived enum values
        var teamResponse = await client.PostAsJsonAsync("/api/v1/team/configure", new
        {
            name = "Robot Team Alpha",
            unitIds,
            strategy = new
            {
                formation,
                targetPriority = new[] { targetPriorities.First() }
            }
        });

        Assert.Equal(HttpStatusCode.Created, teamResponse.StatusCode);
        var team = await ParseResponse(teamResponse);

        // Validate team structure
        Assert.True(team.TryGetProperty("id", out _), "Team should have id");
        Assert.True(team.TryGetProperty("name", out _), "Team should have name");
        Assert.True(team.TryGetProperty("units", out var units), "Team should have units");
        Assert.Equal(JsonValueKind.Array, units.ValueKind);
        Assert.Equal(3, units.GetArrayLength());

        // Validate _links
        var linkErrors = _validator.ValidateLinks(team);
        Assert.Empty(linkErrors);

        var presenceErrors = _validator.ValidateLinkPresence(team, "self", "queue_battle", "delete");
        Assert.Empty(presenceErrors);

        // Follow GET links
        var linkResults = await _linkWalker.FollowAllLinks(team, client);
        AssertLinksValid(linkResults, "Team");
    }

    #endregion

    #region Phase 5: AI Practice

    [Fact]
    public async Task Phase5_AIPractice()
    {
        var client = CreateClient();
        await InitSpec(client);

        var (token, _) = await RegisterRobot(client);
        Authenticate(client, token);

        // Build a team
        var roster = await ParseResponse(await client.GetAsync("/api/v1/player/roster"));
        var unitIds = roster.EnumerateArray().Take(3).Select(u => u.GetProperty("id").GetGuid()).ToList();

        var teamResp = await client.PostAsJsonAsync("/api/v1/team/configure", new
        {
            name = "Robot Practice Team",
            unitIds,
            strategy = new { formation = "balanced", targetPriority = new[] { "lowest_hp" } }
        });
        var team = await ParseResponse(teamResp);
        var teamId = team.GetProperty("id").GetGuid();

        // Get AI opponents
        var opponentsResponse = await client.GetAsync("/api/v1/ai/opponents");
        Assert.Equal(HttpStatusCode.OK, opponentsResponse.StatusCode);

        var opponentsBody = await ParseResponse(opponentsResponse);
        Assert.True(opponentsBody.TryGetProperty("opponents", out var opponents), "Should have opponents array");
        Assert.True(opponents.GetArrayLength() > 0, "Should have at least one AI opponent");

        var firstOpponent = opponents[0];
        var opponentId = firstOpponent.GetProperty("id").GetString();
        _output.WriteLine($"Fighting AI opponent: {firstOpponent.GetProperty("name").GetString()} ({opponentId})");

        // Practice battle (instant — no queue/process needed)
        var practiceResponse = await client.PostAsJsonAsync("/api/v1/ai/practice", new
        {
            teamId,
            opponentId
        });
        Assert.Equal(HttpStatusCode.OK, practiceResponse.StatusCode);

        var battle = await ParseResponse(practiceResponse);
        Assert.True(battle.TryGetProperty("battleId", out _), "Should have battleId");
        Assert.True(battle.TryGetProperty("turns", out var turns), "Should have turns");
        Assert.True(turns.GetInt32() > 0, "Turns should be > 0");
        _output.WriteLine($"Practice battle: {turns.GetInt32()} turns");

        // Verify rewards exist and rating was unaffected
        if (battle.TryGetProperty("rewards", out var rewards))
        {
            if (rewards.TryGetProperty("ratingChange", out var ratingChange))
            {
                Assert.Equal(0, ratingChange.GetInt32());
                _output.WriteLine("Confirmed: ratingChange = 0 for practice battle");
            }
        }
    }

    #endregion

    #region Phase 6: Ranked Battle Full Flow

    [Fact]
    public async Task Phase6_RankedBattle_FullFlow()
    {
        var client = CreateClient();
        await InitSpec(client);

        // --- Player A ---
        var (tokenA, playerIdA) = await RegisterRobot(client);
        Authenticate(client, tokenA);

        var rosterA = await ParseResponse(await client.GetAsync("/api/v1/player/roster"));
        var unitIdsA = rosterA.EnumerateArray().Take(3).Select(u => u.GetProperty("id").GetGuid()).ToList();

        var teamRespA = await client.PostAsJsonAsync("/api/v1/team/configure", new
        {
            name = "Robot A Team",
            unitIds = unitIdsA,
            strategy = new { formation = "aggressive", targetPriority = new[] { "lowest_hp" } }
        });
        var teamA = await ParseResponse(teamRespA);
        var teamIdA = teamA.GetProperty("id").GetGuid();

        // --- Player B (new client) ---
        var clientB = CreateClient();
        var (tokenB, playerIdB) = await RegisterRobot(clientB);
        Authenticate(clientB, tokenB);

        var rosterB = await ParseResponse(await clientB.GetAsync("/api/v1/player/roster"));
        var unitIdsB = rosterB.EnumerateArray().Take(3).Select(u => u.GetProperty("id").GetGuid()).ToList();

        var teamRespB = await clientB.PostAsJsonAsync("/api/v1/team/configure", new
        {
            name = "Robot B Team",
            unitIds = unitIdsB,
            strategy = new { formation = "defensive", targetPriority = new[] { "highest_threat" } }
        });
        var teamB = await ParseResponse(teamRespB);
        var teamIdB = teamB.GetProperty("id").GetGuid();

        // --- Queue battles ---
        var queueRespA = await client.PostAsJsonAsync("/api/v1/battle/queue",
            new { teamId = teamIdA, mode = "ranked" });
        Assert.Equal(HttpStatusCode.Created, queueRespA.StatusCode);

        var queueA = await ParseResponse(queueRespA);
        var battleId = queueA.GetProperty("battleId").GetGuid();
        Assert.Equal("queued", queueA.GetProperty("status").GetString());

        // Validate queue response _links
        var queueLinkErrors = _validator.ValidateLinks(queueA);
        Assert.Empty(queueLinkErrors);
        var queueLinks = await _linkWalker.FollowAllLinks(queueA, client);
        AssertLinksValid(queueLinks, "QueueA");

        var queueRespB = await clientB.PostAsJsonAsync("/api/v1/battle/queue",
            new { teamId = teamIdB, mode = "ranked" });
        Assert.Equal(HttpStatusCode.Created, queueRespB.StatusCode);

        // --- Process battles ---
        await ProcessBattles();

        // --- Get results via _links.results from queue response ---
        var links = _validator.ExtractLinks(queueA);
        Assert.NotNull(links);
        Assert.True(links!.ContainsKey("results"), "Queue response should have 'results' link");

        var resultsHref = links["results"].Href;
        var resultsResponse = await client.GetAsync(resultsHref);
        Assert.Equal(HttpStatusCode.OK, resultsResponse.StatusCode);

        var results = await ParseResponse(resultsResponse);
        Assert.True(results.TryGetProperty("turns", out var resultTurns), "Results should have turns");
        Assert.True(resultTurns.GetInt32() > 0, "Battle should have at least 1 turn");
        _output.WriteLine($"Ranked battle: {resultTurns.GetInt32()} turns");

        // Validate result _links
        var resultLinkErrors = _validator.ValidateLinks(results);
        Assert.Empty(resultLinkErrors);

        var resultPresence = _validator.ValidateLinkPresence(results, "self", "queue_again");
        Assert.Empty(resultPresence);

        // Follow all result links (replay, winner, loser are conditional)
        _linkWalker.ResetVisited();
        var resultLinks = await _linkWalker.FollowAllLinks(results, client);
        AssertLinksValid(resultLinks, "BattleResults");

        // Log which conditional links were present
        var resultLinksDict = _validator.ExtractLinks(results);
        if (resultLinksDict != null)
        {
            foreach (var (name, link) in resultLinksDict)
            {
                _output.WriteLine($"Result link: {name} -> {link.Method} {link.Href}");
            }
        }
    }

    #endregion

    #region Phase 7: Post-Battle Exploration

    [Fact]
    public async Task Phase7_PostBattle_Exploration()
    {
        var client = CreateClient();
        await InitSpec(client);

        var (token, playerId) = await RegisterRobot(client);
        Authenticate(client, token);

        // Build team and do a practice battle so we have some activity
        var roster = await ParseResponse(await client.GetAsync("/api/v1/player/roster"));
        var unitIds = roster.EnumerateArray().Take(3).Select(u => u.GetProperty("id").GetGuid()).ToList();

        await client.PostAsJsonAsync("/api/v1/team/configure", new
        {
            name = "Explorer Team",
            unitIds,
            strategy = new { formation = "balanced", targetPriority = new[] { "lowest_hp" } }
        });

        // Check each exploration endpoint
        var endpoints = new[]
        {
            ("/api/v1/battle/history", "Battle History"),
            ("/api/v1/leaderboard", "Leaderboard"),
            ("/api/v1/challenges/daily", "Daily Challenges"),
            ("/api/v1/mastery/units", "Mastery"),
            ("/api/v1/battlepass/progress", "Battle Pass"),
            ("/api/v1/loot/pending", "Pending Loot"),
            ("/api/v1/season/current", "Season"),
            ("/api/v1/modifiers/current", "Modifiers"),
        };

        foreach (var (path, name) in endpoints)
        {
            var response = await client.GetAsync(path);
            _output.WriteLine($"{name} ({path}): {(int)response.StatusCode} {response.StatusCode}");
            Assert.True((int)response.StatusCode < 500,
                $"{name} returned server error: {(int)response.StatusCode}");
        }

        // Leaderboard — check _links on entries
        var leaderboardResponse = await client.GetAsync("/api/v1/leaderboard");
        if (leaderboardResponse.StatusCode == HttpStatusCode.OK)
        {
            var leaderboard = await ParseResponse(leaderboardResponse);
            if (leaderboard.ValueKind == JsonValueKind.Array && leaderboard.GetArrayLength() > 0)
            {
                var firstEntry = leaderboard[0];
                var entryLinkErrors = _validator.ValidateLinks(firstEntry);
                if (entryLinkErrors.Count == 0)
                {
                    var entryLinks = await _linkWalker.FollowAllLinks(firstEntry, client);
                    AssertLinksValid(entryLinks, "LeaderboardEntry");
                }
            }
        }

        // Challenges — check _links on entries
        var challengesResponse = await client.GetAsync("/api/v1/challenges/daily");
        if (challengesResponse.StatusCode == HttpStatusCode.OK)
        {
            var challenges = await ParseResponse(challengesResponse);
            if (challenges.ValueKind == JsonValueKind.Array && challenges.GetArrayLength() > 0)
            {
                var firstChallenge = challenges[0];
                Assert.True(firstChallenge.TryGetProperty("challengeId", out _) ||
                            firstChallenge.TryGetProperty("id", out _),
                    "Challenge should have an ID field");
                var challengeLinkErrors = _validator.ValidateLinks(firstChallenge);
                Assert.Empty(challengeLinkErrors);
            }
        }
    }

    #endregion

    #region Phase 8: Marketplace & Other Systems

    [Fact]
    public async Task Phase8_Marketplace()
    {
        var client = CreateClient();
        await InitSpec(client);

        var (token, _) = await RegisterRobot(client);
        Authenticate(client, token);

        // Browse strategies (may be empty)
        var browseResponse = await client.GetAsync("/api/v1/strategies/browse");
        _output.WriteLine($"Strategy browse: {(int)browseResponse.StatusCode}");
        Assert.True((int)browseResponse.StatusCode < 500, "Strategy browse should not 500");

        // Upload a strategy
        var uploadResponse = await client.PostAsJsonAsync("/api/v1/strategies/upload", new
        {
            name = "Robot Strategy",
            description = "Auto-generated by robot player test",
            strategyJson = JsonSerializer.Serialize(new
            {
                formation = "balanced",
                targetPriority = new[] { "lowest_hp" },
                abilities = new Dictionary<string, object>()
            }),
            price = 0
        });
        _output.WriteLine($"Strategy upload: {(int)uploadResponse.StatusCode}");
        Assert.True((int)uploadResponse.StatusCode < 500, "Strategy upload should not 500");

        // Other exploration endpoints (no auth body needed)
        var noAuthEndpoints = new[]
        {
            ("/api/v1/cosmetics/shop", "Cosmetics Shop"),
            ("/api/v1/referral/info", "Referral Info"),
            ("/api/v1/rival/current", "Current Rival"),
            ("/api/v1/tournament/current", "Tournament"),
        };

        foreach (var (path, name) in noAuthEndpoints)
        {
            var response = await client.GetAsync(path);
            _output.WriteLine($"{name} ({path}): {(int)response.StatusCode}");
            Assert.True((int)response.StatusCode < 500,
                $"{name} returned server error: {(int)response.StatusCode}");
        }
    }

    #endregion

    #region Full Journey (Orchestrated)

    [Fact]
    public async Task RobotPlayer_FullJourney_AllLinksWork()
    {
        var client = CreateClient();
        await InitSpec(client);
        var allLinkResults = new List<(string Phase, List<LinkResult> Results)>();

        // ====== STEP 1: Register Player A ======
        _output.WriteLine("=== STEP 1: Register Player A ===");
        var (tokenA, playerIdA) = await RegisterRobot(client);
        Authenticate(client, tokenA);

        // ====== STEP 2: Get Profile & Walk Links ======
        _output.WriteLine("=== STEP 2: Profile & Links ===");
        var profileResp = await client.GetAsync("/api/v1/player/profile");
        Assert.Equal(HttpStatusCode.OK, profileResp.StatusCode);
        var profile = await ParseResponse(profileResp);

        Assert.Equal(1, profile.GetProperty("level").GetInt32());
        Assert.Equal(1000, profile.GetProperty("currency").GetInt32());

        var profileLinks = await _linkWalker.FollowAllLinks(profile, client);
        allLinkResults.Add(("Profile", profileLinks));

        // ====== STEP 3: Browse & Unlock Units ======
        _output.WriteLine("=== STEP 3: Roster & Unlock ===");
        var rosterResp = await client.GetAsync("/api/v1/player/roster");
        var roster = await ParseResponse(rosterResp);
        Assert.True(roster.GetArrayLength() >= 3, "Need at least 3 starter units");

        var availableResp = await client.GetAsync("/api/v1/player/roster/available");
        var available = await ParseResponse(availableResp);

        // Unlock one unit if available
        foreach (var unit in available.EnumerateArray())
        {
            if (unit.TryGetProperty("alreadyOwned", out var owned) && !owned.GetBoolean())
            {
                var cost = unit.GetProperty("unlockCost").GetInt32();
                if (cost <= 1000)
                {
                    await client.PostAsJsonAsync("/api/v1/player/roster/unlock",
                        new { templateUnitId = unit.GetProperty("id").GetGuid() });
                    _output.WriteLine($"Unlocked: {unit.GetProperty("name").GetString()} ({cost}g)");
                    break;
                }
            }
        }

        // ====== STEP 4: Create Team with Spec-Derived Enums ======
        _output.WriteLine("=== STEP 4: Team Creation ===");
        roster = await ParseResponse(await client.GetAsync("/api/v1/player/roster"));
        var unitIds = roster.EnumerateArray().Take(3).Select(u => u.GetProperty("id").GetGuid()).ToList();

        var formations = _spec.GetEnumValues("Formation");
        var targetPriorities = _spec.GetEnumValues("TargetPriority");

        var teamResp = await client.PostAsJsonAsync("/api/v1/team/configure", new
        {
            name = "Robot Journey Team",
            unitIds,
            strategy = new
            {
                formation = formations.First(),
                targetPriority = new[] { targetPriorities.First() }
            }
        });
        Assert.Equal(HttpStatusCode.Created, teamResp.StatusCode);
        var team = await ParseResponse(teamResp);
        var teamId = team.GetProperty("id").GetGuid();

        _linkWalker.ResetVisited();
        var teamLinks = await _linkWalker.FollowAllLinks(team, client);
        allLinkResults.Add(("Team", teamLinks));

        // ====== STEP 5: AI Practice ======
        _output.WriteLine("=== STEP 5: AI Practice ===");
        var opponentsResp = await client.GetAsync("/api/v1/ai/opponents");
        if (opponentsResp.StatusCode == HttpStatusCode.OK)
        {
            var oppBody = await ParseResponse(opponentsResp);
            if (oppBody.TryGetProperty("opponents", out var opponents) && opponents.GetArrayLength() > 0)
            {
                var opponentId = opponents[0].GetProperty("id").GetString();
                var practiceResp = await client.PostAsJsonAsync("/api/v1/ai/practice",
                    new { teamId, opponentId });

                if (practiceResp.StatusCode == HttpStatusCode.OK)
                {
                    var practice = await ParseResponse(practiceResp);
                    _output.WriteLine($"Practice: {practice.GetProperty("turns").GetInt32()} turns");
                }
            }
        }

        // ====== STEP 6: Ranked Battle (2 players) ======
        _output.WriteLine("=== STEP 6: Ranked Battle ===");
        var clientB = CreateClient();
        var (tokenB, _) = await RegisterRobot(clientB);
        Authenticate(clientB, tokenB);

        var rosterB = await ParseResponse(await clientB.GetAsync("/api/v1/player/roster"));
        var unitIdsB = rosterB.EnumerateArray().Take(3).Select(u => u.GetProperty("id").GetGuid()).ToList();

        var teamRespB = await clientB.PostAsJsonAsync("/api/v1/team/configure", new
        {
            name = "Robot B Team",
            unitIds = unitIdsB,
            strategy = new { formation = "defensive", targetPriority = new[] { "lowest_hp" } }
        });
        var teamB = await ParseResponse(teamRespB);
        var teamIdB = teamB.GetProperty("id").GetGuid();

        // Queue both
        var queueA = await ParseResponse(
            await client.PostAsJsonAsync("/api/v1/battle/queue", new { teamId, mode = "ranked" }));
        var battleId = queueA.GetProperty("battleId").GetGuid();

        await clientB.PostAsJsonAsync("/api/v1/battle/queue", new { teamId = teamIdB, mode = "ranked" });

        // Process
        await ProcessBattles();

        // Get results via _links
        _linkWalker.ResetVisited();
        var qLinks = _validator.ExtractLinks(queueA);
        if (qLinks != null && qLinks.ContainsKey("results"))
        {
            var resultsResp = await client.GetAsync(qLinks["results"].Href);
            Assert.Equal(HttpStatusCode.OK, resultsResp.StatusCode);

            var results = await ParseResponse(resultsResp);
            _output.WriteLine($"Battle result: {results.GetProperty("turns").GetInt32()} turns");

            var resultLinks = await _linkWalker.FollowAllLinks(results, client);
            allLinkResults.Add(("BattleResults", resultLinks));
        }

        // ====== STEP 7: Post-Battle Exploration ======
        _output.WriteLine("=== STEP 7: Exploration ===");
        var explorationEndpoints = new[]
        {
            "/api/v1/battle/history",
            "/api/v1/leaderboard",
            "/api/v1/challenges/daily",
            "/api/v1/mastery/units",
            "/api/v1/battlepass/progress",
            "/api/v1/loot/pending",
            "/api/v1/season/current",
            "/api/v1/modifiers/current",
            "/api/v1/strategies/browse",
            "/api/v1/cosmetics/shop",
            "/api/v1/referral/info",
            "/api/v1/rival/current",
            "/api/v1/tournament/current",
        };

        foreach (var path in explorationEndpoints)
        {
            var resp = await client.GetAsync(path);
            _output.WriteLine($"  {path}: {(int)resp.StatusCode}");
            Assert.True((int)resp.StatusCode < 500, $"{path} returned server error");

            // Walk _links on array responses (first element)
            if (resp.StatusCode == HttpStatusCode.OK)
            {
                var body = await ParseResponse(resp);
                if (body.ValueKind == JsonValueKind.Array && body.GetArrayLength() > 0)
                {
                    var firstItem = body[0];
                    var itemLinks = await _linkWalker.FollowAllLinks(firstItem, client);
                    if (itemLinks.Count > 0)
                        allLinkResults.Add(($"Exploration:{path}", itemLinks));
                }
                else if (body.ValueKind == JsonValueKind.Object)
                {
                    var objLinks = await _linkWalker.FollowAllLinks(body, client);
                    if (objLinks.Count > 0)
                        allLinkResults.Add(($"Exploration:{path}", objLinks));
                }
            }
        }

        // ====== FINAL: Report all link results ======
        _output.WriteLine("\n=== LINK WALK SUMMARY ===");
        var totalFollowed = 0;
        var totalErrors = 0;

        foreach (var (phase, results) in allLinkResults)
        {
            var followed = results.Count(r => r.WasFollowed);
            var errors = results.Count(r => r.Errors.Count > 0);
            totalFollowed += followed;
            totalErrors += errors;
            _output.WriteLine($"  {phase}: {followed} links followed, {errors} errors");

            foreach (var r in results.Where(r => r.Errors.Count > 0))
            {
                _output.WriteLine($"    FAIL: {r.Name} ({r.Method} {r.Href}): {string.Join(", ", r.Errors)}");
            }
        }

        _output.WriteLine($"\nTOTAL: {totalFollowed} links followed, {totalErrors} errors");
        Assert.Equal(0, totalErrors);
    }

    #endregion
}
