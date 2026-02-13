using System.Net;
using ApiCombatGame.Tests.Helpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ApiCombatGame.Tests.IntegrationTests;

public class AdminPageHandlerTests : IntegrationTestBase
{
    public AdminPageHandlerTests(WebApplicationFactory<Program> factory)
        : base(factory, "Admin")
    {
    }

    [Theory]
    [InlineData("/Admin")]
    [InlineData("/Admin/Players")]
    [InlineData("/Admin/Meta")]
    [InlineData("/Admin/Guilds")]
    [InlineData("/Admin/Technical")]
    [InlineData("/Admin/Tools")]
    public async Task AdminPages_Unauthenticated_RedirectToLogin(string path)
    {
        var client = CreateClientNoRedirect();

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Auth/Login", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task AdminIndex_Unauthenticated_RedirectsToLogin()
    {
        var client = CreateClientNoRedirect();

        var response = await client.GetAsync("/Admin");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Auth/Login", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task AdminToolsPage_Unauthenticated_RedirectsToLogin()
    {
        var client = CreateClientNoRedirect();

        var response = await client.GetAsync("/Admin/Tools");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Auth/Login", response.Headers.Location?.ToString());
    }
}
