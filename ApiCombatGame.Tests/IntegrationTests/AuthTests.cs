using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ApiCombatGame.Data;
using ApiCombatGame.Models.DTOs.Auth;
using ApiCombatGame.Tests.Helpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ApiCombatGame.Tests.IntegrationTests;

public class AuthTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public AuthTests(WebApplicationFactory<Program> factory)
    {
        IntegrationTestSetup.DisableRateLimiting();
        var dbName = $"TestDb_Auth_{Guid.NewGuid()}";
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove the existing DbContext registration
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<GameDbContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                // Add in-memory database for testing
                services.AddDbContext<GameDbContext>(options =>
                    options.UseInMemoryDatabase(dbName));
            });
        }).CreateClient();
    }

    [Fact]
    public async Task Register_WithValidData_ReturnsCreated()
    {
        var request = new RegisterRequest
        {
            Username = $"testuser_{Guid.NewGuid():N}",
            Email = $"test_{Guid.NewGuid():N}@example.com",
            Password = "SecurePass123!"
        };

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", request);
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var authResponse = JsonSerializer.Deserialize<AuthResponse>(content, JsonOptions);
        Assert.NotNull(authResponse);
        Assert.NotEqual(Guid.Empty, authResponse!.PlayerId);
        Assert.False(string.IsNullOrEmpty(authResponse.Token));
    }

    [Fact]
    public async Task Register_WithDuplicateUsername_ReturnsConflict()
    {
        var username = $"duplicate_{Guid.NewGuid():N}";

        var request1 = new RegisterRequest
        {
            Username = username,
            Email = $"test1_{Guid.NewGuid():N}@example.com",
            Password = "SecurePass123!"
        };

        await _client.PostAsJsonAsync("/api/v1/auth/register", request1);

        var request2 = new RegisterRequest
        {
            Username = username,
            Email = $"test2_{Guid.NewGuid():N}@example.com",
            Password = "SecurePass123!"
        };

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", request2);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
        var username = $"logintest_{Guid.NewGuid():N}";
        var password = "SecurePass123!";

        // Register first
        var registerRequest = new RegisterRequest
        {
            Username = username,
            Email = $"login_{Guid.NewGuid():N}@example.com",
            Password = password
        };
        await _client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);

        // Login
        var loginRequest = new LoginRequest
        {
            Username = username,
            Password = password
        };

        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var authResponse = JsonSerializer.Deserialize<AuthResponse>(content, JsonOptions);
        Assert.NotNull(authResponse);
        Assert.False(string.IsNullOrEmpty(authResponse!.Token));
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ReturnsUnauthorized()
    {
        var username = $"badlogin_{Guid.NewGuid():N}";

        var registerRequest = new RegisterRequest
        {
            Username = username,
            Email = $"bad_{Guid.NewGuid():N}@example.com",
            Password = "SecurePass123!"
        };
        await _client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);

        var loginRequest = new LoginRequest
        {
            Username = username,
            Password = "WrongPassword!"
        };

        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/v1/player/profile");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithToken_ReturnsSuccess()
    {
        var token = await RegisterAndGetToken();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.GetAsync("/api/v1/player/profile");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task HealthCheck_ReturnsHealthy()
    {
        var response = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private async Task<string> RegisterAndGetToken()
    {
        var request = new RegisterRequest
        {
            Username = $"authtest_{Guid.NewGuid():N}",
            Email = $"auth_{Guid.NewGuid():N}@example.com",
            Password = "SecurePass123!"
        };

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", request);
        var content = await response.Content.ReadAsStringAsync();
        var authResponse = JsonSerializer.Deserialize<AuthResponse>(content, JsonOptions);
        return authResponse!.Token;
    }
}
