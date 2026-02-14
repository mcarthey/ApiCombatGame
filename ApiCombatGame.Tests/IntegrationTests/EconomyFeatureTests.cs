using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ApiCombatGame.Tests.Helpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ApiCombatGame.Tests.IntegrationTests;

/// <summary>
/// Integration tests for economy features:
/// Loot, Referral, Cosmetics, Premium Plus, Unit Customization, Battle Pass.
/// </summary>
public class EconomyFeatureTests : IntegrationTestBase
{
    public EconomyFeatureTests(WebApplicationFactory<Program> factory)
        : base(factory, "Economy") { }

    // ── Loot ──

    [Fact]
    public async Task Loot_GetPending_NewPlayer_ReturnsOkEmpty()
    {
        var (client, _) = await CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/v1/loot/pending");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(content, JsonOptions);
        Assert.Equal(JsonValueKind.Object, json.ValueKind);
        Assert.Equal(0, json.GetProperty("drops").GetArrayLength());
    }

    [Fact]
    public async Task Loot_Claim_NoPending_ReturnsBadRequest()
    {
        var (client, _) = await CreateAuthenticatedClient();

        var response = await client.PostAsync("/api/v1/loot/claim", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Loot_Unauthenticated_ReturnsUnauthorized()
    {
        var client = CreateClient();

        var response = await client.GetAsync("/api/v1/loot/pending");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── Referral ──

    [Fact]
    public async Task Referral_GetInfo_ReturnsOkWithCode()
    {
        var (client, _) = await CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/v1/referral/info");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(content, JsonOptions);
        Assert.True(json.GetProperty("referralCode").GetString()!.Length > 0);
        Assert.Equal(0, json.GetProperty("totalReferrals").GetInt32());
    }

    [Fact]
    public async Task Referral_Redeem_InvalidCode_ReturnsNotFound()
    {
        var (client, _) = await CreateAuthenticatedClient();

        var response = await client.PostAsync("/api/v1/referral/redeem/INVALIDCODE", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Referral_Redeem_OwnCode_ReturnsBadRequest()
    {
        var (client, _) = await CreateAuthenticatedClient();

        // Get own code
        var infoResponse = await client.GetAsync("/api/v1/referral/info");
        var info = JsonSerializer.Deserialize<JsonElement>(
            await infoResponse.Content.ReadAsStringAsync(), JsonOptions);
        var ownCode = info.GetProperty("referralCode").GetString();

        // Try to redeem own code
        var response = await client.PostAsync($"/api/v1/referral/redeem/{ownCode}", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Referral_GetLeaderboard_ReturnsOk()
    {
        var (client, _) = await CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/v1/referral/leaderboard");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Referral_Flow_TwoPlayers_RedeemSucceeds()
    {
        // Player A gets referral code
        var (clientA, _) = await CreateAuthenticatedClient();
        var infoResponse = await clientA.GetAsync("/api/v1/referral/info");
        var info = JsonSerializer.Deserialize<JsonElement>(
            await infoResponse.Content.ReadAsStringAsync(), JsonOptions);
        var codeA = info.GetProperty("referralCode").GetString();

        // Player B redeems Player A's code
        var (clientB, _) = await CreateAuthenticatedClient();
        var redeemResponse = await clientB.PostAsync($"/api/v1/referral/redeem/{codeA}", null);

        Assert.Equal(HttpStatusCode.OK, redeemResponse.StatusCode);
    }

    // ── Cosmetics ──

    [Fact]
    public async Task Cosmetics_GetShop_ReturnsOkWithItems()
    {
        var (client, _) = await CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/v1/cosmetics/shop");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(content, JsonOptions);
        Assert.Equal(JsonValueKind.Object, json.ValueKind);
        Assert.True(json.TryGetProperty("items", out _));
        Assert.True(json.TryGetProperty("playerGems", out _));
    }

    [Fact]
    public async Task Cosmetics_GetOwned_NewPlayer_ReturnsOkEmpty()
    {
        var (client, _) = await CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/v1/cosmetics/owned");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(content, JsonOptions);
        Assert.Equal(JsonValueKind.Array, json.ValueKind);
        Assert.Equal(0, json.GetArrayLength());
    }

    [Fact]
    public async Task Cosmetics_GetBalance_ReturnsOk()
    {
        var (client, _) = await CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/v1/cosmetics/balance");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(content, JsonOptions);
        Assert.True(json.TryGetProperty("gems", out _));
        Assert.True(json.TryGetProperty("gold", out _));
    }

    [Fact]
    public async Task Cosmetics_Purchase_NonExistent_ReturnsNotFoundOrBadRequest()
    {
        var (client, _) = await CreateAuthenticatedClient();

        var request = new { };
        var response = await client.PostAsJsonAsync($"/api/v1/cosmetics/purchase/{Guid.NewGuid()}", request);

        Assert.True(
            response.StatusCode == HttpStatusCode.NotFound ||
            response.StatusCode == HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Cosmetics_Equip_NotOwned_ReturnsBadRequest()
    {
        var (client, _) = await CreateAuthenticatedClient();

        var response = await client.PostAsync($"/api/v1/cosmetics/equip/{Guid.NewGuid()}", null);

        Assert.True(
            response.StatusCode == HttpStatusCode.BadRequest ||
            response.StatusCode == HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Cosmetics_Unauthenticated_ReturnsUnauthorized()
    {
        var client = CreateClient();

        var response = await client.GetAsync("/api/v1/cosmetics/shop");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── Premium Plus ──

    [Fact]
    public async Task Premium_GetPerks_ReturnsOkWithTierInfo()
    {
        var (client, _) = await CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/v1/premium/perks");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(content, JsonOptions);
        Assert.True(json.TryGetProperty("currentTier", out _));
        Assert.True(json.TryGetProperty("goldMultiplier", out _));
    }

    [Fact]
    public async Task Premium_ClaimGems_FreeTier_ReturnsBadRequest()
    {
        var (client, _) = await CreateAuthenticatedClient();

        var response = await client.PostAsync("/api/v1/premium/claim-gems", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Premium_GetBadges_ReturnsOkArray()
    {
        var (client, _) = await CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/v1/premium/badges");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(content, JsonOptions);
        Assert.Equal(JsonValueKind.Array, json.ValueKind);
    }

    [Fact]
    public async Task Premium_SetBadge_InvalidBadge_ReturnsBadRequest()
    {
        var (client, _) = await CreateAuthenticatedClient();

        var request = new { badge = "nonexistent_badge" };
        var response = await client.PostAsJsonAsync("/api/v1/premium/badge", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Premium_Unauthenticated_ReturnsUnauthorized()
    {
        var client = CreateClient();

        var response = await client.GetAsync("/api/v1/premium/perks");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── Unit Customization ──

    [Fact]
    public async Task UnitCustomization_Rename_NonExistentUnit_ReturnsNotFoundOrBadRequest()
    {
        var (client, _) = await CreateAuthenticatedClient();

        var request = new { unitId = Guid.NewGuid(), newName = "TestName" };
        var response = await client.PostAsJsonAsync("/api/v1/units/rename", request);

        Assert.True(
            response.StatusCode == HttpStatusCode.NotFound ||
            response.StatusCode == HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UnitCustomization_Reroll_NonExistentUnit_ReturnsNotFoundOrBadRequest()
    {
        var (client, _) = await CreateAuthenticatedClient();

        var request = new { unitId = Guid.NewGuid(), stat = "attack" };
        var response = await client.PostAsJsonAsync("/api/v1/units/reroll", request);

        Assert.True(
            response.StatusCode == HttpStatusCode.NotFound ||
            response.StatusCode == HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UnitCustomization_GoldenUpgrade_NonExistentUnit_ReturnsNotFoundOrBadRequest()
    {
        var (client, _) = await CreateAuthenticatedClient();

        var request = new { unitId = Guid.NewGuid() };
        var response = await client.PostAsJsonAsync("/api/v1/units/golden-upgrade", request);

        Assert.True(
            response.StatusCode == HttpStatusCode.NotFound ||
            response.StatusCode == HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UnitCustomization_Unauthenticated_ReturnsUnauthorized()
    {
        var client = CreateClient();

        var request = new { unitId = Guid.NewGuid(), newName = "Test" };
        var response = await client.PostAsJsonAsync("/api/v1/units/rename", request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── Battle Pass ──

    [Fact]
    public async Task BattlePass_GetProgress_ReturnsOk()
    {
        var (client, _) = await CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/v1/battlepass/progress");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(content, JsonOptions);
        Assert.True(json.TryGetProperty("currentLevel", out _) || json.TryGetProperty("level", out _));
    }

    [Fact]
    public async Task BattlePass_ClaimLevel_NotReached_ReturnsBadRequest()
    {
        var (client, _) = await CreateAuthenticatedClient();

        var response = await client.PostAsync("/api/v1/battlepass/claim/30", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task BattlePass_Unauthenticated_ReturnsUnauthorized()
    {
        var client = CreateClient();

        var response = await client.GetAsync("/api/v1/battlepass/progress");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── Rival ──

    [Fact]
    public async Task Rival_GetCurrent_ReturnsOkWithRivalInfo()
    {
        var (client, _) = await CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/v1/rival/current");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(content, JsonOptions);
        Assert.True(json.TryGetProperty("hasRival", out _));
    }

    [Fact]
    public async Task Rival_Unauthenticated_ReturnsUnauthorized()
    {
        var client = CreateClient();

        var response = await client.GetAsync("/api/v1/rival/current");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
