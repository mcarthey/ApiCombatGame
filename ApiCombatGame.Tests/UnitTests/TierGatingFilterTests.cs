using System.Security.Claims;
using ApiCombatGame.Data;
using ApiCombatGame.Filters;
using ApiCombatGame.Models.Domain;
using ApiCombatGame.Models.Enums;
using ApiCombatGame.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Xunit;

namespace ApiCombatGame.Tests.UnitTests;

public class TierGatingFilterTests
{
    private static ActionExecutingContext CreateContext(
        GameDbContext context,
        ClaimsPrincipal? user = null,
        RequiresTierAttribute? attribute = null)
    {
        var httpContext = new DefaultHttpContext();
        if (user != null)
            httpContext.User = user;

        // Build a minimal ActionDescriptor with endpoint metadata
        var actionDescriptor = new ActionDescriptor();
        if (attribute != null)
        {
            actionDescriptor.EndpointMetadata = new List<object> { attribute };
        }
        else
        {
            actionDescriptor.EndpointMetadata = new List<object>();
        }

        var actionContext = new ActionContext(httpContext, new RouteData(), actionDescriptor);

        return new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new Dictionary<string, object?>(),
            new object());
    }

    private static ClaimsPrincipal CreateUser(Guid playerId)
    {
        var claims = new[]
        {
            new Claim("PlayerId", playerId.ToString()),
            new Claim(ClaimTypes.NameIdentifier, playerId.ToString())
        };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
    }

    [Fact]
    public async Task NoAttribute_ProceedsWithoutCheck()
    {
        var db = TestDbContextFactory.Create();
        var filter = new TierGatingActionFilter(db);
        var ctx = CreateContext(db);
        var nextCalled = false;

        await filter.OnActionExecutionAsync(ctx, () =>
        {
            nextCalled = true;
            return Task.FromResult<ActionExecutedContext>(null!);
        });

        Assert.True(nextCalled);
        Assert.Null(ctx.Result);
    }

    [Fact]
    public async Task FreeTier_BlockedByPremiumRequirement()
    {
        var db = TestDbContextFactory.Create();
        var player = TestDbContextFactory.CreatePlayer(db, "freeuser", SubscriptionTier.Free);
        var filter = new TierGatingActionFilter(db);
        var user = CreateUser(player.Id);
        var attr = new RequiresTierAttribute(SubscriptionTier.Premium);
        var ctx = CreateContext(db, user, attr);
        var nextCalled = false;

        await filter.OnActionExecutionAsync(ctx, () =>
        {
            nextCalled = true;
            return Task.FromResult<ActionExecutedContext>(null!);
        });

        Assert.False(nextCalled);
        Assert.NotNull(ctx.Result);
        var result = ctx.Result as ObjectResult;
        Assert.Equal(403, result!.StatusCode);
    }

    [Fact]
    public async Task PremiumTier_AllowedByPremiumRequirement()
    {
        var db = TestDbContextFactory.Create();
        var player = TestDbContextFactory.CreatePlayer(db, "premuser", SubscriptionTier.Premium);
        var filter = new TierGatingActionFilter(db);
        var user = CreateUser(player.Id);
        var attr = new RequiresTierAttribute(SubscriptionTier.Premium);
        var ctx = CreateContext(db, user, attr);
        var nextCalled = false;

        await filter.OnActionExecutionAsync(ctx, () =>
        {
            nextCalled = true;
            return Task.FromResult<ActionExecutedContext>(null!);
        });

        Assert.True(nextCalled);
        Assert.Null(ctx.Result);
    }

    [Fact]
    public async Task PremiumPlusTier_AllowedByPremiumRequirement()
    {
        var db = TestDbContextFactory.Create();
        var player = TestDbContextFactory.CreatePlayer(db, "ppuser", SubscriptionTier.PremiumPlus);
        var filter = new TierGatingActionFilter(db);
        var user = CreateUser(player.Id);
        var attr = new RequiresTierAttribute(SubscriptionTier.Premium);
        var ctx = CreateContext(db, user, attr);
        var nextCalled = false;

        await filter.OnActionExecutionAsync(ctx, () =>
        {
            nextCalled = true;
            return Task.FromResult<ActionExecutedContext>(null!);
        });

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task PremiumTier_BlockedByPremiumPlusRequirement()
    {
        var db = TestDbContextFactory.Create();
        var player = TestDbContextFactory.CreatePlayer(db, "premblocked", SubscriptionTier.Premium);
        var filter = new TierGatingActionFilter(db);
        var user = CreateUser(player.Id);
        var attr = new RequiresTierAttribute(SubscriptionTier.PremiumPlus);
        var ctx = CreateContext(db, user, attr);
        var nextCalled = false;

        await filter.OnActionExecutionAsync(ctx, () =>
        {
            nextCalled = true;
            return Task.FromResult<ActionExecutedContext>(null!);
        });

        Assert.False(nextCalled);
        var result = ctx.Result as ObjectResult;
        Assert.Equal(403, result!.StatusCode);
    }

    [Fact]
    public async Task MissingPlayerId_ReturnsUnauthorized()
    {
        var db = TestDbContextFactory.Create();
        var filter = new TierGatingActionFilter(db);
        var user = new ClaimsPrincipal(new ClaimsIdentity(Array.Empty<Claim>(), "TestAuth"));
        var attr = new RequiresTierAttribute(SubscriptionTier.Premium);
        var ctx = CreateContext(db, user, attr);

        await filter.OnActionExecutionAsync(ctx, () => Task.FromResult<ActionExecutedContext>(null!));

        Assert.NotNull(ctx.Result);
        Assert.IsType<UnauthorizedObjectResult>(ctx.Result);
    }

    [Fact]
    public async Task PlayerNotFound_ReturnsNotFound()
    {
        var db = TestDbContextFactory.Create();
        var filter = new TierGatingActionFilter(db);
        var user = CreateUser(Guid.NewGuid()); // Non-existent player
        var attr = new RequiresTierAttribute(SubscriptionTier.Premium);
        var ctx = CreateContext(db, user, attr);

        await filter.OnActionExecutionAsync(ctx, () => Task.FromResult<ActionExecutedContext>(null!));

        Assert.NotNull(ctx.Result);
        Assert.IsType<NotFoundObjectResult>(ctx.Result);
    }

    [Fact]
    public async Task ForbiddenResponse_ContainsTierInfo()
    {
        var db = TestDbContextFactory.Create();
        var player = TestDbContextFactory.CreatePlayer(db, "infouser", SubscriptionTier.Free);
        var filter = new TierGatingActionFilter(db);
        var user = CreateUser(player.Id);
        var attr = new RequiresTierAttribute(SubscriptionTier.Premium);
        var ctx = CreateContext(db, user, attr);

        await filter.OnActionExecutionAsync(ctx, () => Task.FromResult<ActionExecutedContext>(null!));

        var result = ctx.Result as ObjectResult;
        Assert.Equal(403, result!.StatusCode);

        // Verify the response contains tier info (it's an anonymous object)
        var json = System.Text.Json.JsonSerializer.Serialize(result.Value);
        Assert.Contains("Premium", json);
        Assert.Contains("Free", json);
        Assert.Contains("upgradeUrl", json);
    }
}
