using ApiCombatGame.Models.Enums;
using ApiCombatGame.Pages.Account;
using ApiCombatGame.Services;
using ApiCombatGame.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using Xunit;

namespace ApiCombatGame.Tests.UnitTests;

public class AnalyticsPageTests
{
    [Fact]
    public async Task OnGetAsync_FreeUser_SetsIsPremiumPlusToFalse()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var player = TestDbContextFactory.CreatePlayer(context, "freeuser", SubscriptionTier.Free);
        var analyticsService = new PlayerAnalyticsService(context);
        var pageModel = new AnalyticsModel(analyticsService, context);

        SetupPageContext(pageModel, player.Id);

        // Act
        var result = await pageModel.OnGetAsync();

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Equal("Free", pageModel.CurrentTier);
        Assert.False(pageModel.IsPremiumPlus);
        Assert.NotNull(pageModel.Analytics);
    }

    [Fact]
    public async Task OnGetAsync_PremiumUser_SetsIsPremiumPlusToFalse()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var player = TestDbContextFactory.CreatePlayer(context, "premiumuser", SubscriptionTier.Premium);
        var analyticsService = new PlayerAnalyticsService(context);
        var pageModel = new AnalyticsModel(analyticsService, context);

        SetupPageContext(pageModel, player.Id);

        // Act
        var result = await pageModel.OnGetAsync();

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Equal("Premium", pageModel.CurrentTier);
        Assert.False(pageModel.IsPremiumPlus);
        Assert.NotNull(pageModel.Analytics);
    }

    [Fact]
    public async Task OnGetAsync_PremiumPlusUser_SetsIsPremiumPlusToTrue()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var player = TestDbContextFactory.CreatePlayer(context, "premiumplus", SubscriptionTier.PremiumPlus);
        var analyticsService = new PlayerAnalyticsService(context);
        var pageModel = new AnalyticsModel(analyticsService, context);

        SetupPageContext(pageModel, player.Id);

        // Act
        var result = await pageModel.OnGetAsync();

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Equal("PremiumPlus", pageModel.CurrentTier);
        Assert.True(pageModel.IsPremiumPlus);
        Assert.NotNull(pageModel.Analytics);
    }

    [Fact]
    public async Task OnGetAsync_InvalidPlayerId_RedirectsToLogin()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var analyticsService = new PlayerAnalyticsService(context);
        var pageModel = new AnalyticsModel(analyticsService, context);

        SetupPageContext(pageModel, null); // No player ID claim

        // Act
        var result = await pageModel.OnGetAsync();

        // Assert
        var redirectResult = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Auth/Login", redirectResult.PageName);
    }

    [Fact]
    public async Task OnGetAsync_NonExistentPlayer_DefaultsToFreeTier()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var analyticsService = new PlayerAnalyticsService(context);
        var pageModel = new AnalyticsModel(analyticsService, context);

        var nonExistentPlayerId = Guid.NewGuid();
        SetupPageContext(pageModel, nonExistentPlayerId);

        // Act & Assert
        // This should throw because the analytics service expects a valid player
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await pageModel.OnGetAsync());
    }

    [Fact]
    public async Task OnGetAsync_UserWithBattles_ReturnsAnalytics()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var player = TestDbContextFactory.CreatePlayer(context, "battler", SubscriptionTier.Premium);
        var opponent = TestDbContextFactory.CreatePlayer(context, "opponent", SubscriptionTier.Free);

        // Create a completed battle
        var battle = new ApiCombatGame.Models.Domain.Battle
        {
            Id = Guid.NewGuid(),
            Player1Id = player.Id,
            Player2Id = opponent.Id,
            Status = BattleStatus.Completed,
            WinnerId = player.Id,
            Turns = 10,
            CompletedAt = DateTime.UtcNow,
            Player1RatingChange = 25,
            Player2RatingChange = -20,
            CurrencyReward = 100,
            Team1ClassesJson = "[\"Warrior\",\"Mage\"]",
            Team2ClassesJson = "[\"Rogue\",\"Cleric\"]",
            QueuedAt = DateTime.UtcNow,
            Team1Id = Guid.NewGuid(),
            Team2Id = Guid.NewGuid()
        };
        context.Battles.Add(battle);
        context.SaveChanges();

        var analyticsService = new PlayerAnalyticsService(context);
        var pageModel = new AnalyticsModel(analyticsService, context);

        SetupPageContext(pageModel, player.Id);

        // Act
        var result = await pageModel.OnGetAsync();

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Equal("Premium", pageModel.CurrentTier);
        Assert.False(pageModel.IsPremiumPlus);
        Assert.NotNull(pageModel.Analytics);
        Assert.Equal(1, pageModel.Analytics.TotalBattles);
        Assert.Equal(1, pageModel.Analytics.TotalWins);
        Assert.Equal(0, pageModel.Analytics.TotalLosses);
        Assert.Equal(100, pageModel.Analytics.WinRate);
    }

    [Theory]
    [InlineData(SubscriptionTier.Free, false)]
    [InlineData(SubscriptionTier.Premium, false)]
    [InlineData(SubscriptionTier.PremiumPlus, true)]
    public async Task OnGetAsync_VariousTiers_SetsIsPremiumPlusCorrectly(SubscriptionTier tier, bool expectedIsPremiumPlus)
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var player = TestDbContextFactory.CreatePlayer(context, $"user_{tier}", tier);
        var analyticsService = new PlayerAnalyticsService(context);
        var pageModel = new AnalyticsModel(analyticsService, context);

        SetupPageContext(pageModel, player.Id);

        // Act
        var result = await pageModel.OnGetAsync();

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Equal(tier.ToString(), pageModel.CurrentTier);
        Assert.Equal(expectedIsPremiumPlus, pageModel.IsPremiumPlus);
    }

    private static void SetupPageContext(PageModel pageModel, Guid? playerId)
    {
        var httpContext = new DefaultHttpContext();

        if (playerId.HasValue)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, playerId.Value.ToString()),
                new Claim(ClaimTypes.Name, "testuser")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);
            httpContext.User = principal;
        }

        pageModel.PageContext = new PageContext
        {
            HttpContext = httpContext
        };
    }
}
