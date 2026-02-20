using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ApiCombatGame.Tests.Helpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ApiCombatGame.Tests.IntegrationTests;

/// <summary>
/// Integration tests for platform features:
/// Education, SDK, Discord, Content Creator, Activity Feed.
/// </summary>
public class PlatformFeatureTests : IntegrationTestBase
{
    public PlatformFeatureTests(WebApplicationFactory<Program> factory)
        : base(factory, "Platform") { }

    // ── SDK (No Auth Required) ──

    [Fact]
    public async Task Sdk_GetQuickstart_ReturnsOkWithGuide()
    {
        var client = CreateClient();

        var response = await client.GetAsync("/api/v1/sdk/quickstart");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(content, JsonOptions);
        Assert.True(json.TryGetProperty("steps", out _));
    }

    [Fact]
    public async Task Sdk_GetEndpoints_ReturnsOkWithCatalog()
    {
        var client = CreateClient();

        var response = await client.GetAsync("/api/v1/sdk/endpoints");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(content, JsonOptions);
        Assert.True(json.TryGetProperty("categories", out _));
    }

    [Fact]
    public async Task Sdk_GetStatus_ReturnsOkWithMetrics()
    {
        var client = CreateClient();

        var response = await client.GetAsync("/api/v1/sdk/status");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(content, JsonOptions);
        Assert.True(json.TryGetProperty("status", out _));
        Assert.True(json.TryGetProperty("metrics", out _));
    }

    // ── Activity Feed ──

    [Fact]
    public async Task Activity_GetOwnFeed_NewPlayer_ReturnsOkEmpty()
    {
        var (client, _) = await CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/v1/activity/feed");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(content, JsonOptions);
        Assert.True(json.TryGetProperty("activities", out var activities));
        Assert.Equal(0, activities.GetArrayLength());
    }

    [Fact]
    public async Task Activity_GetPublicFeed_ExistingPlayer_ReturnsOk()
    {
        var (client, auth) = await CreateAuthenticatedClient();

        // Fetch own feed by username (should work since we just registered)
        var profileResponse = await client.GetAsync("/api/v1/player/profile");
        var profile = JsonSerializer.Deserialize<JsonElement>(
            await profileResponse.Content.ReadAsStringAsync(), JsonOptions);
        var username = profile.GetProperty("username").GetString();

        var response = await client.GetAsync($"/api/v1/activity/feed/{username}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Activity_GetPublicFeed_NonExistent_ReturnsNotFound()
    {
        var (client, _) = await CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/v1/activity/feed/nonexistent_player_xyz");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Activity_GetOwnStats_ReturnsOkWithDefaults()
    {
        var (client, _) = await CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/v1/activity/stats");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(content, JsonOptions);
        Assert.Equal(0, json.GetProperty("totalBattlesPlayed").GetInt32());
    }

    [Fact]
    public async Task Activity_GetPublicStats_NonExistent_ReturnsNotFound()
    {
        var (client, _) = await CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/v1/activity/stats/nonexistent_player_xyz");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Activity_Unauthenticated_ReturnsUnauthorized()
    {
        var client = CreateClient();

        var response = await client.GetAsync("/api/v1/activity/feed");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── Education ──

    [Fact]
    public async Task Education_GetModules_ReturnsOkArray()
    {
        var (client, _) = await CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/v1/education/modules");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(content, JsonOptions);
        Assert.Equal(JsonValueKind.Array, json.ValueKind);
    }

    [Fact]
    public async Task Education_CreateModule_ReturnsCreated()
    {
        var (client, _) = await CreateEducatorClient();

        var request = new
        {
            title = "Test Module",
            description = "A test curriculum module",
            difficulty = "beginner",
            lessons = new[]
            {
                new { title = "Lesson 1", objective = "Learn to register", endpoint = "POST /api/v1/auth/register", hint = "Use valid data" },
                new { title = "Lesson 2", objective = "Learn to login", endpoint = "POST /api/v1/auth/login", hint = "Use your credentials" }
            }
        };

        var response = await client.PostAsJsonAsync("/api/v1/education/modules", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(content, JsonOptions);
        Assert.Equal("Test Module", json.GetProperty("title").GetString());
        Assert.True(json.GetProperty("joinCode").GetString()!.Length > 0);
    }

    [Fact]
    public async Task Education_GetModuleDetail_NonExistent_ReturnsNotFound()
    {
        var (client, _) = await CreateAuthenticatedClient();

        var response = await client.GetAsync($"/api/v1/education/modules/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Education_Enroll_NonExistent_ReturnsNotFound()
    {
        var (client, _) = await CreateAuthenticatedClient();

        var response = await client.PostAsync($"/api/v1/education/enroll/{Guid.NewGuid()}", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Education_EnrollByCode_InvalidCode_ReturnsNotFound()
    {
        var (client, _) = await CreateAuthenticatedClient();

        var response = await client.PostAsync("/api/v1/education/enroll/code/BADCODE1", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Education_GetMyProgress_ReturnsOkArray()
    {
        var (client, _) = await CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/v1/education/my-progress");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(content, JsonOptions);
        Assert.Equal(JsonValueKind.Array, json.ValueKind);
    }

    [Fact]
    public async Task Education_InstructorDashboard_ReturnsOk()
    {
        var (client, _) = await CreateEducatorClient();

        var response = await client.GetAsync("/api/v1/education/instructor/dashboard");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Education_FullFlow_CreatePublishEnrollComplete()
    {
        // Instructor creates module (requires educator privileges)
        var (instructor, _) = await CreateEducatorClient();
        var createRequest = new
        {
            title = "Flow Test Module",
            description = "Testing full education flow",
            difficulty = "beginner",
            lessons = new[]
            {
                new { title = "Step 1", objective = "Do the thing", endpoint = "GET /api/v1/player/profile", hint = "Just call it" }
            }
        };
        var createResponse = await instructor.PostAsJsonAsync("/api/v1/education/modules", createRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var module = JsonSerializer.Deserialize<JsonElement>(
            await createResponse.Content.ReadAsStringAsync(), JsonOptions);
        var moduleId = module.GetProperty("id").GetGuid();
        var joinCode = module.GetProperty("joinCode").GetString();

        // Publish module
        var publishResponse = await instructor.PostAsync($"/api/v1/education/modules/{moduleId}/publish", null);
        Assert.Equal(HttpStatusCode.OK, publishResponse.StatusCode);

        // Student enrolls by code
        var (student, _) = await CreateAuthenticatedClient();
        var enrollResponse = await student.PostAsync($"/api/v1/education/enroll/code/{joinCode}", null);
        Assert.Equal(HttpStatusCode.OK, enrollResponse.StatusCode);

        // Student completes lesson
        var completeResponse = await student.PostAsync($"/api/v1/education/modules/{moduleId}/lessons/0/complete", null);
        Assert.Equal(HttpStatusCode.OK, completeResponse.StatusCode);
        var progress = JsonSerializer.Deserialize<JsonElement>(
            await completeResponse.Content.ReadAsStringAsync(), JsonOptions);
        Assert.True(progress.GetProperty("isCompleted").GetBoolean());
    }

    [Fact]
    public async Task Education_Unauthenticated_ReturnsUnauthorized()
    {
        var client = CreateClient();

        var response = await client.GetAsync("/api/v1/education/modules");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── Educator Gating ──

    [Fact]
    public async Task Education_CreateModule_NonEducator_Returns403()
    {
        var (client, _) = await CreateAuthenticatedClient();

        var request = new
        {
            title = "Blocked Module",
            description = "Should not be created",
            difficulty = "beginner",
            lessons = new[]
            {
                new { title = "L1", objective = "Obj", endpoint = "GET /api/v1/player/profile", hint = "Hint" }
            }
        };

        var response = await client.PostAsJsonAsync("/api/v1/education/modules", request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Education_CreateModule_EducatorFlag_Succeeds()
    {
        var (client, _) = await CreateEducatorClient();

        var request = new
        {
            title = "Educator Module",
            description = "Created by an educator",
            difficulty = "beginner",
            lessons = new[]
            {
                new { title = "L1", objective = "Obj", endpoint = "GET /api/v1/player/profile", hint = "Hint" }
            }
        };

        var response = await client.PostAsJsonAsync("/api/v1/education/modules", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Education_InstructorDashboard_NonEducator_Returns403()
    {
        var (client, _) = await CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/v1/education/instructor/dashboard");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ── Education Phase 6: Class Leaderboard ──

    [Fact]
    public async Task Education_ModuleLeaderboard_NotEnrolled_ReturnsBadRequest()
    {
        var (client, _) = await CreateAuthenticatedClient();

        var response = await client.GetAsync($"/api/v1/education/modules/{Guid.NewGuid()}/leaderboard");

        // 404 for nonexistent module or 400 for not enrolled
        Assert.True(
            response.StatusCode == HttpStatusCode.NotFound ||
            response.StatusCode == HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Education_ModuleLeaderboard_Enrolled_ReturnsOk()
    {
        var (client, auth) = await CreateEducatorClient();

        // Create and publish module
        var moduleRequest = new
        {
            title = "Leaderboard Test Module",
            description = "Testing leaderboard",
            difficulty = "beginner",
            lessons = new[] { new { title = "L1", objective = "Test", endpoint = "GET /api/v1/player/profile", hint = "" } }
        };
        var createResponse = await client.PostAsJsonAsync("/api/v1/education/modules", moduleRequest);
        createResponse.EnsureSuccessStatusCode();
        var json = JsonSerializer.Deserialize<JsonElement>(await createResponse.Content.ReadAsStringAsync(), JsonOptions);
        var moduleId = json.GetProperty("id").GetGuid();

        await client.PostAsync($"/api/v1/education/modules/{moduleId}/publish", null);
        await client.PostAsync($"/api/v1/education/enroll/{moduleId}", null);

        var response = await client.GetAsync($"/api/v1/education/modules/{moduleId}/leaderboard");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        var leaderboard = JsonSerializer.Deserialize<JsonElement>(content, JsonOptions);
        Assert.Equal(JsonValueKind.Array, leaderboard.ValueKind);
        Assert.True(leaderboard.GetArrayLength() >= 1);
    }

    // ── Education Phase 6: Unenroll ──

    [Fact]
    public async Task Education_Unenroll_NotEnrolled_ReturnsNotFound()
    {
        var (client, _) = await CreateAuthenticatedClient();

        var response = await client.DeleteAsync($"/api/v1/education/enroll/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ── Education Phase 6: Class Tournament ──

    [Fact]
    public async Task Education_CreateClassTournament_NonEducator_Returns403()
    {
        var (client, _) = await CreateAuthenticatedClient();

        var request = new { entryFee = 0, maxParticipants = 8 };
        var response = await client.PostAsJsonAsync($"/api/v1/education/modules/{Guid.NewGuid()}/tournament", request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ── AI Batch Practice ──

    [Fact]
    public async Task Ai_BatchPractice_InvalidTeam_ReturnsBadRequest()
    {
        var (client, _) = await CreateAuthenticatedClient();

        var request = new { teamId = Guid.NewGuid(), opponentId = "novice-1", count = 5 };
        var response = await client.PostAsJsonAsync("/api/v1/ai/batch-practice", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ── Discord ──

    [Fact]
    public async Task Discord_GetLink_NewPlayer_ReturnsSuccessNoLink()
    {
        var (client, _) = await CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/v1/discord/link");

        // Ok(null) returns 204 NoContent for ActionResult<T?>
        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Discord_LinkAccount_ReturnsOkWithVerificationCode()
    {
        var (client, _) = await CreateAuthenticatedClient();

        var request = new { discordUserId = "123456789012345678", discordUsername = "testuser#1234" };
        var response = await client.PostAsJsonAsync("/api/v1/discord/link", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(content, JsonOptions);
        Assert.True(json.TryGetProperty("verificationCode", out _));
    }

    [Fact]
    public async Task Discord_Verify_InvalidCode_ReturnsBadRequest()
    {
        var (client, _) = await CreateAuthenticatedClient();

        // First link
        var linkRequest = new { discordUserId = "123456789012345678", discordUsername = "testuser#1234" };
        await client.PostAsJsonAsync("/api/v1/discord/link", linkRequest);

        // Try invalid code
        var verifyRequest = new { verificationCode = "000000" };
        var response = await client.PostAsJsonAsync("/api/v1/discord/verify", verifyRequest);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Discord_Unlink_NoLink_ReturnsNotFound()
    {
        var (client, _) = await CreateAuthenticatedClient();

        var response = await client.DeleteAsync("/api/v1/discord/link");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Discord_GetWebhooks_ReturnsOkEmpty()
    {
        var (client, _) = await CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/v1/discord/webhooks");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(content, JsonOptions);
        Assert.Equal(JsonValueKind.Array, json.ValueKind);
        Assert.Equal(0, json.GetArrayLength());
    }

    [Fact]
    public async Task Discord_RegisterWebhook_ReturnsCreated()
    {
        var (client, _) = await CreateAuthenticatedClient();

        var request = new
        {
            webhookUrl = "https://discord.com/api/webhooks/123/abc",
            label = "Test Webhook",
            eventTypes = new[] { "battle_won", "battle_lost", "level_up" }
        };
        var response = await client.PostAsJsonAsync("/api/v1/discord/webhooks", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Discord_DeleteWebhook_NonExistent_ReturnsNotFound()
    {
        var (client, _) = await CreateAuthenticatedClient();

        var response = await client.DeleteAsync($"/api/v1/discord/webhooks/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Discord_Lookup_NoLink_ReturnsNotFound()
    {
        var (client, _) = await CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/v1/discord/lookup/999999999999999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Discord_Unauthenticated_ReturnsUnauthorized()
    {
        var client = CreateClient();

        var response = await client.GetAsync("/api/v1/discord/link");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── Content Creator ──

    [Fact]
    public async Task Creator_GetProfile_NewPlayer_ReturnsSuccessNoProfile()
    {
        var (client, _) = await CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/v1/creators/me");

        // Ok(null) returns 204 NoContent for ActionResult<T?>
        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Creator_Apply_ReturnsCreated()
    {
        var (client, _) = await CreateAuthenticatedClient();

        var request = new { creatorName = "TestCreator", bio = "I create strategies" };
        var response = await client.PostAsJsonAsync("/api/v1/creators/apply", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(content, JsonOptions);
        Assert.Equal("TestCreator", json.GetProperty("creatorName").GetString());
    }

    [Fact]
    public async Task Creator_Apply_Duplicate_ReturnsBadRequest()
    {
        var (client, _) = await CreateAuthenticatedClient();

        var request = new { creatorName = "TestCreator", bio = "I create strategies" };
        await client.PostAsJsonAsync("/api/v1/creators/apply", request);

        // Second application should fail
        var response = await client.PostAsJsonAsync("/api/v1/creators/apply", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Creator_GetStats_NotCreator_ReturnsNotFound()
    {
        var (client, _) = await CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/v1/creators/stats");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Creator_GetVerified_ReturnsOkArray()
    {
        var (client, _) = await CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/v1/creators/verified");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(content, JsonOptions);
        Assert.Equal(JsonValueKind.Array, json.ValueKind);
    }

    [Fact]
    public async Task Creator_GetSpotlight_ReturnsOk()
    {
        var (client, _) = await CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/v1/creators/spotlight");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Creator_Unauthenticated_ReturnsUnauthorized()
    {
        var client = CreateClient();

        var response = await client.GetAsync("/api/v1/creators/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
