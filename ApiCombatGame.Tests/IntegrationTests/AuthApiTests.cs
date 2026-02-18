using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ApiCombatGame.Models.DTOs.Auth;
using ApiCombatGame.Tests.Helpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ApiCombatGame.Tests.IntegrationTests;

public class AuthApiTests : IntegrationTestBase
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public AuthApiTests(WebApplicationFactory<Program> factory) : base(factory, "AuthApi") { }

    [Fact]
    public async Task Register_Returns201_WithTokenAndPlayerId()
    {
        var client = CreateClient();
        var request = new RegisterRequest
        {
            Username = $"regtest_{Guid.NewGuid():N}",
            Email = $"regtest_{Guid.NewGuid():N}@test.com",
            Password = "SecurePass123!"
        };

        var response = await client.PostAsJsonAsync("/api/v1/auth/register", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var auth = JsonSerializer.Deserialize<AuthResponse>(content, Json)!;
        Assert.NotEqual(Guid.Empty, auth.PlayerId);
        Assert.False(string.IsNullOrEmpty(auth.Token));
    }

    [Fact]
    public async Task Register_StarterUnitsInRoster()
    {
        var (client, auth) = await CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/v1/player/roster");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var roster = JsonSerializer.Deserialize<List<JsonElement>>(content, Json)!;

        Assert.Equal(3, roster.Count);

        // BUG REGRESSION: Each unit should have abilities
        foreach (var unit in roster)
        {
            Assert.True(unit.TryGetProperty("abilities", out var abilities),
                "Unit should have abilities property");
            Assert.True(abilities.GetArrayLength() > 0,
                $"Unit should have at least one ability");
        }
    }

    [Fact]
    public async Task Login_Returns200_WithToken()
    {
        var client = CreateClient();
        var username = $"logintest_{Guid.NewGuid():N}";
        var email = $"{username}@test.com";
        var password = "SecurePass123!";

        // Register first
        await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest
        {
            Username = username,
            Email = email,
            Password = password
        });

        // Login
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
        {
            Email = email,
            Password = password
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var auth = JsonSerializer.Deserialize<AuthResponse>(content, Json)!;
        Assert.False(string.IsNullOrEmpty(auth.Token));
    }

    [Fact]
    public async Task Login_InvalidPassword_Returns401()
    {
        var client = CreateClient();
        var email = $"badlogin_{Guid.NewGuid():N}@test.com";

        await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest
        {
            Username = $"badlogin_{Guid.NewGuid():N}",
            Email = email,
            Password = "SecurePass123!"
        });

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
        {
            Email = email,
            Password = "WrongPassword!"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ApiEndpoint_NoToken_Returns401Json()
    {
        // BUG REGRESSION: Unauthenticated API calls were returning HTML redirect instead of 401 JSON
        var client = CreateClient();

        var response = await client.GetAsync("/api/v1/player/roster");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("<html", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ApiEndpoint_InvalidToken_Returns401Json()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "completely.invalid.token");

        var response = await client.GetAsync("/api/v1/player/roster");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("<html", content, StringComparison.OrdinalIgnoreCase);
    }
}
