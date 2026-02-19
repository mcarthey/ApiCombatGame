using ApiCombatGame.PlaywrightTests.Helpers;
using Microsoft.Playwright;
using Xunit;
using Xunit.Abstractions;

namespace ApiCombatGame.PlaywrightTests.SmokeTests;

/// <summary>
/// Smoke tests for public-facing web pages. Verifies pages load without crashing,
/// key elements render, and auth redirects work. Catches the 20% of web surface
/// that would immediately turn off new players.
/// </summary>
public class PublicPageTests : PlaywrightTestBase
{
    private readonly ITestOutputHelper _output;

    public PublicPageTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task HomePage_LoadsWithKeyElements()
    {
        var page = await NewPageAsync();
        var response = await page.GotoAsync(BaseUrl);

        Assert.NotNull(response);
        Assert.Equal(200, response!.Status);

        // Title should contain API Combat
        var title = await page.TitleAsync();
        Assert.Contains("API Combat", title, StringComparison.OrdinalIgnoreCase);
        _output.WriteLine($"Page title: {title}");

        // Hero section should render
        var hero = page.Locator(".hero-section");
        Assert.True(await hero.CountAsync() > 0, "Hero section should exist");

        // H1 heading
        var h1 = await page.Locator("h1").First.TextContentAsync();
        Assert.False(string.IsNullOrEmpty(h1), "H1 should have content");
        _output.WriteLine($"H1: {h1?.Trim()}");
    }

    [Fact]
    public async Task ApiDocsPage_LoadsWithEndpoints()
    {
        var page = await NewPageAsync();
        var response = await page.GotoAsync($"{BaseUrl}/api-docs/v1");

        Assert.NotNull(response);
        Assert.Equal(200, response!.Status);

        // Should have at least some endpoint content
        var title = await page.TitleAsync();
        _output.WriteLine($"API Docs title: {title}");

        // Wait for content to render
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // The page should have some content (not be blank)
        var bodyText = await page.Locator("body").TextContentAsync();
        Assert.True(bodyText?.Length > 100, "API docs page should have substantial content");
    }

    [Fact]
    public async Task LeaderboardPage_Loads()
    {
        var page = await NewPageAsync();
        var response = await page.GotoAsync($"{BaseUrl}/Leaderboard");

        Assert.NotNull(response);
        Assert.Equal(200, response!.Status);

        var title = await page.TitleAsync();
        _output.WriteLine($"Leaderboard title: {title}");
    }

    [Fact]
    public async Task LoginPage_HasFormElements()
    {
        var page = await NewPageAsync();
        var response = await page.GotoAsync($"{BaseUrl}/Auth/Login");

        Assert.NotNull(response);
        Assert.Equal(200, response!.Status);

        // Email field
        var emailInput = page.Locator("input[type='email'], input[name='Email'], input[name='Input.Email']");
        Assert.True(await emailInput.CountAsync() > 0, "Login should have email field");

        // Password field
        var passwordInput = page.Locator("input[type='password']");
        Assert.True(await passwordInput.CountAsync() > 0, "Login should have password field");

        // Submit button
        var submitBtn = page.Locator("button[type='submit'], input[type='submit']");
        Assert.True(await submitBtn.CountAsync() > 0, "Login should have submit button");
    }

    [Fact]
    public async Task RegisterPage_HasFormElements()
    {
        var page = await NewPageAsync();
        var response = await page.GotoAsync($"{BaseUrl}/Auth/Register");

        Assert.NotNull(response);
        Assert.Equal(200, response!.Status);

        // Should have username, email, and password fields
        var inputs = page.Locator("input");
        var inputCount = await inputs.CountAsync();
        Assert.True(inputCount >= 3, $"Register should have at least 3 input fields, found {inputCount}");

        // Password field
        var passwordInput = page.Locator("input[type='password']");
        Assert.True(await passwordInput.CountAsync() > 0, "Register should have password field");
    }

    [Theory]
    [InlineData("/About")]
    [InlineData("/Privacy")]
    [InlineData("/Terms")]
    [InlineData("/Contact")]
    public async Task StaticPages_LoadSuccessfully(string path)
    {
        var page = await NewPageAsync();
        var response = await page.GotoAsync($"{BaseUrl}{path}");

        Assert.NotNull(response);
        Assert.Equal(200, response!.Status);

        var title = await page.TitleAsync();
        _output.WriteLine($"{path} title: {title}");
    }

    [Fact]
    public async Task Dashboard_RedirectsToLogin_WhenNotAuthenticated()
    {
        var page = await NewPageAsync();
        await page.GotoAsync($"{BaseUrl}/Dashboard");

        // Should have been redirected to login
        Assert.Contains("/Auth/Login", page.Url, StringComparison.OrdinalIgnoreCase);
        _output.WriteLine($"Redirected to: {page.Url}");
    }

    [Fact]
    public async Task NonExistentPage_DoesNotCrash()
    {
        var page = await NewPageAsync();
        var response = await page.GotoAsync($"{BaseUrl}/this-page-does-not-exist-xyz123");

        Assert.NotNull(response);
        // Should return 404 or render error page (not 500 server error)
        Assert.True(response!.Status != 500,
            $"Non-existent page should not return 500, got {response.Status}");
        _output.WriteLine($"404 test: status={response.Status}");
    }
}
