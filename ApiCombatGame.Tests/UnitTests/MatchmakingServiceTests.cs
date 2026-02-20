using ApiCombatGame.Data;
using ApiCombatGame.Models.Domain;
using ApiCombatGame.Models.Enums;
using ApiCombatGame.Services;
using ApiCombatGame.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ApiCombatGame.Tests.UnitTests;

public class MatchmakingServiceTests
{
    private GameDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<GameDbContext>()
            .UseInMemoryDatabase($"TestDb_Matchmaking_{Guid.NewGuid()}")
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new GameDbContext(options);
    }

    private Mock<IBotTeamGenerator> CreateMockBotTeamGenerator()
    {
        var mock = new Mock<IBotTeamGenerator>();
        // Setup default behavior - bots will have teams ready
        mock.Setup(x => x.EnsureBotHasTeamAsync(It.IsAny<Player>()))
            .Returns(Task.CompletedTask);
        return mock;
    }

    [Fact]
    public async Task FindMatchAsync_PremiumUserGetsMatchedFirst()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var logger = new Mock<ILogger<MatchmakingService>>();
        var botTeamGenerator = CreateMockBotTeamGenerator();
        var service = new MatchmakingService(context, logger.Object, botTeamGenerator.Object);

        var freePlayer = new Player
        {
            Id = Guid.NewGuid(),
            Username = "free_player",
            Email = "free@test.com",
            Rating = 1000,
            CurrentTier = SubscriptionTier.Free
        };

        var premiumPlayer = new Player
        {
            Id = Guid.NewGuid(),
            Username = "premium_player",
            Email = "premium@test.com",
            Rating = 1000,
            CurrentTier = SubscriptionTier.Premium
        };

        var freeBattle = new Battle
        {
            Id = Guid.NewGuid(),
            Player1Id = freePlayer.Id,
            Player1 = freePlayer,
            Status = BattleStatus.Queued,
            QueuedAt = DateTime.UtcNow.AddSeconds(-5) // Queued first
        };

        var premiumBattle = new Battle
        {
            Id = Guid.NewGuid(),
            Player1Id = premiumPlayer.Id,
            Player1 = premiumPlayer,
            Status = BattleStatus.Queued,
            QueuedAt = DateTime.UtcNow // Queued second but should get priority
        };

        var thirdPlayer = new Player
        {
            Id = Guid.NewGuid(),
            Username = "third_player",
            Email = "third@test.com",
            Rating = 1000,
            CurrentTier = SubscriptionTier.Free
        };

        var thirdBattle = new Battle
        {
            Id = Guid.NewGuid(),
            Player1Id = thirdPlayer.Id,
            Player1 = thirdPlayer,
            Status = BattleStatus.Queued,
            QueuedAt = DateTime.UtcNow
        };

        context.Players.AddRange(freePlayer, premiumPlayer, thirdPlayer);
        context.Battles.AddRange(freeBattle, premiumBattle, thirdBattle);
        await context.SaveChangesAsync();

        // Act
        var match = await service.FindMatchAsync();

        // Assert
        Assert.NotNull(match);
        // Premium player should be matched first despite queuing later
        Assert.True(
            match.Value.battle1.Player1Id == premiumPlayer.Id ||
            match.Value.battle2.Player1Id == premiumPlayer.Id,
            "Premium player should be in the match");
    }

    [Fact]
    public async Task FindMatchAsync_PremiumGetsTighterRatingRange()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var logger = new Mock<ILogger<MatchmakingService>>();
        var botTeamGenerator = CreateMockBotTeamGenerator();
        var service = new MatchmakingService(context, logger.Object, botTeamGenerator.Object);

        var premiumPlayer = new Player
        {
            Id = Guid.NewGuid(),
            Username = "premium_player",
            Email = "premium@test.com",
            Rating = 1000,
            CurrentTier = SubscriptionTier.Premium
        };

        var farOpponent = new Player
        {
            Id = Guid.NewGuid(),
            Username = "far_opponent",
            Email = "far@test.com",
            Rating = 1250, // 250 points away - outside Premium range (200) but within Free range (300)
            CurrentTier = SubscriptionTier.Free
        };

        var premiumBattle = new Battle
        {
            Id = Guid.NewGuid(),
            Player1Id = premiumPlayer.Id,
            Player1 = premiumPlayer,
            Status = BattleStatus.Queued,
            QueuedAt = DateTime.UtcNow.AddSeconds(-5) // Short wait, no expansion yet
        };

        var opponentBattle = new Battle
        {
            Id = Guid.NewGuid(),
            Player1Id = farOpponent.Id,
            Player1 = farOpponent,
            Status = BattleStatus.Queued,
            QueuedAt = DateTime.UtcNow.AddSeconds(-5)
        };

        context.Players.AddRange(premiumPlayer, farOpponent);
        context.Battles.AddRange(premiumBattle, opponentBattle);
        await context.SaveChangesAsync();

        // Act
        var match = await service.FindMatchAsync();

        // Assert - Should NOT match because rating difference (250) exceeds Premium range (200)
        Assert.Null(match);
    }

    [Fact]
    public async Task FindMatchAsync_FreeUserMatchesWiderRange()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var logger = new Mock<ILogger<MatchmakingService>>();
        var botTeamGenerator = CreateMockBotTeamGenerator();
        var service = new MatchmakingService(context, logger.Object, botTeamGenerator.Object);

        var freePlayer = new Player
        {
            Id = Guid.NewGuid(),
            Username = "free_player",
            Email = "free@test.com",
            Rating = 1000,
            CurrentTier = SubscriptionTier.Free
        };

        var opponent = new Player
        {
            Id = Guid.NewGuid(),
            Username = "opponent",
            Email = "opponent@test.com",
            Rating = 1250, // 250 points away - within Free range (300)
            CurrentTier = SubscriptionTier.Free
        };

        var freeBattle = new Battle
        {
            Id = Guid.NewGuid(),
            Player1Id = freePlayer.Id,
            Player1 = freePlayer,
            Status = BattleStatus.Queued,
            QueuedAt = DateTime.UtcNow.AddSeconds(-5)
        };

        var opponentBattle = new Battle
        {
            Id = Guid.NewGuid(),
            Player1Id = opponent.Id,
            Player1 = opponent,
            Status = BattleStatus.Queued,
            QueuedAt = DateTime.UtcNow.AddSeconds(-5)
        };

        context.Players.AddRange(freePlayer, opponent);
        context.Battles.AddRange(freeBattle, opponentBattle);
        await context.SaveChangesAsync();

        // Act
        var match = await service.FindMatchAsync();

        // Assert - Should match because rating difference (250) is within Free range (300)
        Assert.NotNull(match);
    }

    [Fact]
    public async Task FindMatchAsync_PremiumFasterForceMatchTimeout()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var logger = new Mock<ILogger<MatchmakingService>>();
        var botTeamGenerator = CreateMockBotTeamGenerator();
        var service = new MatchmakingService(context, logger.Object, botTeamGenerator.Object);

        var premiumPlayer = new Player
        {
            Id = Guid.NewGuid(),
            Username = "premium_player",
            Email = "premium@test.com",
            Rating = 1000,
            CurrentTier = SubscriptionTier.Premium
        };

        var farOpponent = new Player
        {
            Id = Guid.NewGuid(),
            Username = "far_opponent",
            Email = "far@test.com",
            Rating = 2000, // Very far rating
            CurrentTier = SubscriptionTier.Free
        };

        var premiumBattle = new Battle
        {
            Id = Guid.NewGuid(),
            Player1Id = premiumPlayer.Id,
            Player1 = premiumPlayer,
            Status = BattleStatus.Queued,
            QueuedAt = DateTime.UtcNow.AddSeconds(-25) // 25s wait - past Premium threshold (20s) but not Free (30s)
        };

        var opponentBattle = new Battle
        {
            Id = Guid.NewGuid(),
            Player1Id = farOpponent.Id,
            Player1 = farOpponent,
            Status = BattleStatus.Queued,
            QueuedAt = DateTime.UtcNow.AddSeconds(-25)
        };

        context.Players.AddRange(premiumPlayer, farOpponent);
        context.Battles.AddRange(premiumBattle, opponentBattle);
        await context.SaveChangesAsync();

        // Act
        var match = await service.FindMatchAsync();

        // Assert - Should force-match because Premium player waited > 20s
        Assert.NotNull(match);
    }

    [Fact]
    public async Task FindMatchAsync_NoMatchWithLessThanTwoPlayers()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var logger = new Mock<ILogger<MatchmakingService>>();
        var botTeamGenerator = CreateMockBotTeamGenerator();
        var service = new MatchmakingService(context, logger.Object, botTeamGenerator.Object);

        var player = new Player
        {
            Id = Guid.NewGuid(),
            Username = "solo_player",
            Email = "solo@test.com",
            Rating = 1000,
            CurrentTier = SubscriptionTier.Free
        };

        var battle = new Battle
        {
            Id = Guid.NewGuid(),
            Player1Id = player.Id,
            Player1 = player,
            Status = BattleStatus.Queued,
            QueuedAt = DateTime.UtcNow
        };

        context.Players.Add(player);
        context.Battles.Add(battle);
        await context.SaveChangesAsync();

        // Act
        var match = await service.FindMatchAsync();

        // Assert
        Assert.Null(match);
    }

    [Fact]
    public async Task FindMatchAsync_PremiumPlusGetsSameBenefitsAsPremium()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var logger = new Mock<ILogger<MatchmakingService>>();
        var botTeamGenerator = CreateMockBotTeamGenerator();
        var service = new MatchmakingService(context, logger.Object, botTeamGenerator.Object);

        var premiumPlusPlayer = new Player
        {
            Id = Guid.NewGuid(),
            Username = "premiumplus_player",
            Email = "premiumplus@test.com",
            Rating = 1000,
            CurrentTier = SubscriptionTier.PremiumPlus
        };

        var freePlayer = new Player
        {
            Id = Guid.NewGuid(),
            Username = "free_player",
            Email = "free@test.com",
            Rating = 1000,
            CurrentTier = SubscriptionTier.Free
        };

        var premiumPlusBattle = new Battle
        {
            Id = Guid.NewGuid(),
            Player1Id = premiumPlusPlayer.Id,
            Player1 = premiumPlusPlayer,
            Status = BattleStatus.Queued,
            QueuedAt = DateTime.UtcNow // Queued later
        };

        var freeBattle = new Battle
        {
            Id = Guid.NewGuid(),
            Player1Id = freePlayer.Id,
            Player1 = freePlayer,
            Status = BattleStatus.Queued,
            QueuedAt = DateTime.UtcNow.AddSeconds(-10) // Queued first
        };

        var thirdPlayer = new Player
        {
            Id = Guid.NewGuid(),
            Username = "third",
            Email = "third@test.com",
            Rating = 1000,
            CurrentTier = SubscriptionTier.Free
        };

        var thirdBattle = new Battle
        {
            Id = Guid.NewGuid(),
            Player1Id = thirdPlayer.Id,
            Player1 = thirdPlayer,
            Status = BattleStatus.Queued,
            QueuedAt = DateTime.UtcNow
        };

        context.Players.AddRange(premiumPlusPlayer, freePlayer, thirdPlayer);
        context.Battles.AddRange(premiumPlusBattle, freeBattle, thirdBattle);
        await context.SaveChangesAsync();

        // Act
        var match = await service.FindMatchAsync();

        // Assert
        Assert.NotNull(match);
        // Premium Plus player should get priority
        Assert.True(
            match.Value.battle1.Player1Id == premiumPlusPlayer.Id ||
            match.Value.battle2.Player1Id == premiumPlusPlayer.Id,
            "Premium Plus player should get matched with priority");
    }

    [Fact]
    public async Task FindMatchAsync_SinglePlayerGetsBot_AfterWaitThreshold()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var logger = new Mock<ILogger<MatchmakingService>>();
        var botTeamGenerator = CreateMockBotTeamGenerator();
        var service = new MatchmakingService(context, logger.Object, botTeamGenerator.Object);

        var player = new Player
        {
            Id = Guid.NewGuid(),
            Username = "solo_player",
            Email = "solo@test.com",
            Rating = 1000,
            CurrentTier = SubscriptionTier.Free
        };

        var bot = new Player
        {
            Id = Guid.NewGuid(),
            Username = "BotWarrior",
            Email = "bot@test.com",
            Rating = 1050,
            IsBot = true,
            CurrentTier = SubscriptionTier.Free
        };

        var botTeam = new Team
        {
            Id = Guid.NewGuid(),
            PlayerId = bot.Id,
            Name = "Bot Team"
        };

        var battle = new Battle
        {
            Id = Guid.NewGuid(),
            Player1Id = player.Id,
            Player1 = player,
            Status = BattleStatus.Queued,
            QueuedAt = DateTime.UtcNow.AddSeconds(-20) // Past the 15s Free threshold
        };

        context.Players.AddRange(player, bot);
        context.Teams.Add(botTeam);
        context.Battles.Add(battle);
        await context.SaveChangesAsync();

        // Act
        var match = await service.FindMatchAsync();

        // Assert - Single player waiting > 15s should be matched with a bot
        Assert.NotNull(match);
        Assert.True(
            match.Value.battle1.Player1Id == bot.Id ||
            match.Value.battle2.Player1Id == bot.Id,
            "Solo player should be matched with a bot after wait threshold");
    }

    [Fact]
    public async Task FindMatchAsync_SinglePlayerNoBot_BelowWaitThreshold()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var logger = new Mock<ILogger<MatchmakingService>>();
        var botTeamGenerator = CreateMockBotTeamGenerator();
        var service = new MatchmakingService(context, logger.Object, botTeamGenerator.Object);

        var player = new Player
        {
            Id = Guid.NewGuid(),
            Username = "solo_player",
            Email = "solo@test.com",
            Rating = 1000,
            CurrentTier = SubscriptionTier.Free
        };

        var bot = new Player
        {
            Id = Guid.NewGuid(),
            Username = "BotWarrior",
            Email = "bot@test.com",
            Rating = 1050,
            IsBot = true,
            CurrentTier = SubscriptionTier.Free
        };

        var botTeam = new Team
        {
            Id = Guid.NewGuid(),
            PlayerId = bot.Id,
            Name = "Bot Team"
        };

        var battle = new Battle
        {
            Id = Guid.NewGuid(),
            Player1Id = player.Id,
            Player1 = player,
            Status = BattleStatus.Queued,
            QueuedAt = DateTime.UtcNow.AddSeconds(-5) // Only 5s, below 15s threshold
        };

        context.Players.AddRange(player, bot);
        context.Teams.Add(botTeam);
        context.Battles.Add(battle);
        await context.SaveChangesAsync();

        // Act
        var match = await service.FindMatchAsync();

        // Assert - Should NOT match yet, wait threshold not met
        Assert.Null(match);
    }

    [Fact]
    public async Task FindMatchAsync_PremiumSinglePlayer_FasterBotThreshold()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var logger = new Mock<ILogger<MatchmakingService>>();
        var botTeamGenerator = CreateMockBotTeamGenerator();
        var service = new MatchmakingService(context, logger.Object, botTeamGenerator.Object);

        var premiumPlayer = new Player
        {
            Id = Guid.NewGuid(),
            Username = "premium_solo",
            Email = "premium@test.com",
            Rating = 1000,
            CurrentTier = SubscriptionTier.Premium
        };

        var bot = new Player
        {
            Id = Guid.NewGuid(),
            Username = "BotDefender",
            Email = "bot@test.com",
            Rating = 1100,
            IsBot = true,
            CurrentTier = SubscriptionTier.Free
        };

        var botTeam = new Team
        {
            Id = Guid.NewGuid(),
            PlayerId = bot.Id,
            Name = "Bot Team"
        };

        var battle = new Battle
        {
            Id = Guid.NewGuid(),
            Player1Id = premiumPlayer.Id,
            Player1 = premiumPlayer,
            Status = BattleStatus.Queued,
            QueuedAt = DateTime.UtcNow.AddSeconds(-12) // 12s: past Premium threshold (10s) but below Free (15s)
        };

        context.Players.AddRange(premiumPlayer, bot);
        context.Teams.Add(botTeam);
        context.Battles.Add(battle);
        await context.SaveChangesAsync();

        // Act
        var match = await service.FindMatchAsync();

        // Assert - Premium player should get bot match faster (10s threshold)
        Assert.NotNull(match);
        Assert.True(
            match.Value.battle1.Player1Id == bot.Id ||
            match.Value.battle2.Player1Id == bot.Id,
            "Premium solo player should get bot match at 10s threshold");
    }

    // ==================== Additional Coverage ====================

    [Fact]
    public async Task FindMatch_TwoPlayers_RatingWithinRange_Matches()
    {
        var context = CreateInMemoryContext();
        var logger = new Mock<ILogger<MatchmakingService>>();
        var botTeamGenerator = CreateMockBotTeamGenerator();
        var service = new MatchmakingService(context, logger.Object, botTeamGenerator.Object);

        var p1 = new Player { Id = Guid.NewGuid(), Username = "p1", Email = "p1@test.com", Rating = 1000, CurrentTier = SubscriptionTier.Free };
        var p2 = new Player { Id = Guid.NewGuid(), Username = "p2", Email = "p2@test.com", Rating = 1100, CurrentTier = SubscriptionTier.Free };

        context.Players.AddRange(p1, p2);
        context.Battles.Add(new Battle { Id = Guid.NewGuid(), Player1Id = p1.Id, Player1 = p1, Status = BattleStatus.Queued, QueuedAt = DateTime.UtcNow.AddSeconds(-5) });
        context.Battles.Add(new Battle { Id = Guid.NewGuid(), Player1Id = p2.Id, Player1 = p2, Status = BattleStatus.Queued, QueuedAt = DateTime.UtcNow.AddSeconds(-5) });
        await context.SaveChangesAsync();

        var match = await service.FindMatchAsync();

        Assert.NotNull(match);
    }

    [Fact]
    public async Task FindMatch_TwoPlayers_RatingTooFar_NoMatch()
    {
        var context = CreateInMemoryContext();
        var logger = new Mock<ILogger<MatchmakingService>>();
        var botTeamGenerator = CreateMockBotTeamGenerator();
        var service = new MatchmakingService(context, logger.Object, botTeamGenerator.Object);

        var p1 = new Player { Id = Guid.NewGuid(), Username = "close", Email = "close@test.com", Rating = 500, CurrentTier = SubscriptionTier.Free };
        var p2 = new Player { Id = Guid.NewGuid(), Username = "far", Email = "far@test.com", Rating = 2000, CurrentTier = SubscriptionTier.Free };

        context.Players.AddRange(p1, p2);
        context.Battles.Add(new Battle { Id = Guid.NewGuid(), Player1Id = p1.Id, Player1 = p1, Status = BattleStatus.Queued, QueuedAt = DateTime.UtcNow.AddSeconds(-2) });
        context.Battles.Add(new Battle { Id = Guid.NewGuid(), Player1Id = p2.Id, Player1 = p2, Status = BattleStatus.Queued, QueuedAt = DateTime.UtcNow.AddSeconds(-2) });
        await context.SaveChangesAsync();

        var match = await service.FindMatchAsync();

        // Rating difference of 1500 should exceed all ranges at short wait time
        Assert.Null(match);
    }

    [Fact]
    public async Task FindMatch_WaitExpansion_ExpandsRangeOverTime()
    {
        var context = CreateInMemoryContext();
        var logger = new Mock<ILogger<MatchmakingService>>();
        var botTeamGenerator = CreateMockBotTeamGenerator();
        var service = new MatchmakingService(context, logger.Object, botTeamGenerator.Object);

        var p1 = new Player { Id = Guid.NewGuid(), Username = "waiting", Email = "waiting@test.com", Rating = 1000, CurrentTier = SubscriptionTier.Free };
        var p2 = new Player { Id = Guid.NewGuid(), Username = "far_opp", Email = "far_opp@test.com", Rating = 1400, CurrentTier = SubscriptionTier.Free };

        context.Players.AddRange(p1, p2);
        // Long wait time = expanded range (base 300 + wait bonus)
        context.Battles.Add(new Battle { Id = Guid.NewGuid(), Player1Id = p1.Id, Player1 = p1, Status = BattleStatus.Queued, QueuedAt = DateTime.UtcNow.AddSeconds(-25) });
        context.Battles.Add(new Battle { Id = Guid.NewGuid(), Player1Id = p2.Id, Player1 = p2, Status = BattleStatus.Queued, QueuedAt = DateTime.UtcNow.AddSeconds(-25) });
        await context.SaveChangesAsync();

        var match = await service.FindMatchAsync();

        // 25s wait → waitBonus = (25/10) * 50 = 100, effectiveRange = 300 + 100 = 400
        // Rating diff = 400, should match
        Assert.NotNull(match);
    }

    [Fact]
    public async Task FindMatch_ForceMatch_After30sForFree()
    {
        var context = CreateInMemoryContext();
        var logger = new Mock<ILogger<MatchmakingService>>();
        var botTeamGenerator = CreateMockBotTeamGenerator();
        var service = new MatchmakingService(context, logger.Object, botTeamGenerator.Object);

        var p1 = new Player { Id = Guid.NewGuid(), Username = "long_wait", Email = "long@test.com", Rating = 500, CurrentTier = SubscriptionTier.Free };
        var p2 = new Player { Id = Guid.NewGuid(), Username = "also_long", Email = "also@test.com", Rating = 2500, CurrentTier = SubscriptionTier.Free };

        context.Players.AddRange(p1, p2);
        context.Battles.Add(new Battle { Id = Guid.NewGuid(), Player1Id = p1.Id, Player1 = p1, Status = BattleStatus.Queued, QueuedAt = DateTime.UtcNow.AddSeconds(-35) });
        context.Battles.Add(new Battle { Id = Guid.NewGuid(), Player1Id = p2.Id, Player1 = p2, Status = BattleStatus.Queued, QueuedAt = DateTime.UtcNow.AddSeconds(-35) });
        await context.SaveChangesAsync();

        var match = await service.FindMatchAsync();

        // After 30s, Free tier gets force-matched regardless of rating
        Assert.NotNull(match);
    }

    [Fact]
    public async Task FindMatch_SamePlayerTwice_Skipped()
    {
        var context = CreateInMemoryContext();
        var logger = new Mock<ILogger<MatchmakingService>>();
        var botTeamGenerator = CreateMockBotTeamGenerator();
        var service = new MatchmakingService(context, logger.Object, botTeamGenerator.Object);

        var p1 = new Player { Id = Guid.NewGuid(), Username = "duper", Email = "duper@test.com", Rating = 1000, CurrentTier = SubscriptionTier.Free };

        context.Players.Add(p1);
        // Same player queued twice (shouldn't happen but defensive check)
        context.Battles.Add(new Battle { Id = Guid.NewGuid(), Player1Id = p1.Id, Player1 = p1, Status = BattleStatus.Queued, QueuedAt = DateTime.UtcNow.AddSeconds(-5) });
        context.Battles.Add(new Battle { Id = Guid.NewGuid(), Player1Id = p1.Id, Player1 = p1, Status = BattleStatus.Queued, QueuedAt = DateTime.UtcNow.AddSeconds(-3) });
        await context.SaveChangesAsync();

        var match = await service.FindMatchAsync();

        // Should not match player against themselves
        Assert.Null(match);
    }
}
