using ApiCombatGame.Data;
using ApiCombatGame.Models.Domain;
using ApiCombatGame.Models.Enums;
using ApiCombatGame.Services;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Xunit;

namespace ApiCombatGame.Tests.UnitTests;

public class PlayerAnalyticsServiceTests
{
    private GameDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<GameDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new GameDbContext(options);
    }

    [Fact]
    public async Task GetPlayerAnalyticsAsync_NoPlayerFound_ThrowsException()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var service = new PlayerAnalyticsService(context);
        var nonExistentPlayerId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.GetPlayerAnalyticsAsync(nonExistentPlayerId));
    }

    [Fact]
    public async Task GetPlayerAnalyticsAsync_PlayerWithNoBattles_ReturnsEmptyAnalytics()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var player = new Player
        {
            Id = Guid.NewGuid(),
            Username = "TestPlayer",
            Email = "test@example.com",
            PasswordHash = "hash",
            Rating = 1000,
            Currency = 1000
        };
        context.Players.Add(player);
        await context.SaveChangesAsync();

        var service = new PlayerAnalyticsService(context);

        // Act
        var result = await service.GetPlayerAnalyticsAsync(player.Id);

        // Assert
        Assert.Equal(0, result.TotalBattles);
        Assert.Equal(0, result.TotalWins);
        Assert.Equal(0, result.TotalLosses);
        Assert.Equal(0, result.WinRate);
        Assert.Empty(result.RecentBattles);
        Assert.Empty(result.MostUsedClasses);
        Assert.Empty(result.WinRateByClass);
    }

    [Fact]
    public async Task GetPlayerAnalyticsAsync_CalculatesBasicStats_Correctly()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var player = new Player
        {
            Id = Guid.NewGuid(),
            Username = "TestPlayer",
            Email = "test@example.com",
            PasswordHash = "hash",
            Rating = 1200,
            Currency = 2000
        };
        var opponent = new Player
        {
            Id = Guid.NewGuid(),
            Username = "Opponent",
            Email = "opponent@example.com",
            PasswordHash = "hash",
            Rating = 1100,
            Currency = 1000
        };
        context.Players.AddRange(player, opponent);
        await context.SaveChangesAsync();

        // Create 5 battles: 3 wins, 2 losses
        var battles = new List<Battle>
        {
            // Win 1
            new Battle
            {
                Id = Guid.NewGuid(),
                Player1Id = player.Id,
                Player2Id = opponent.Id,
                WinnerId = player.Id,
                Status = BattleStatus.Completed,
                Turns = 10,
                Player1RatingChange = 25,
                Player2RatingChange = -25,
                CurrencyReward = 100,
                QueuedAt = DateTime.UtcNow.AddDays(-5),
                CompletedAt = DateTime.UtcNow.AddDays(-5),
                Team1ClassesJson = JsonSerializer.Serialize(new[] { "Warrior", "Mage" }),
                Team2ClassesJson = JsonSerializer.Serialize(new[] { "Tank", "Healer" })
            },
            // Win 2
            new Battle
            {
                Id = Guid.NewGuid(),
                Player1Id = player.Id,
                Player2Id = opponent.Id,
                WinnerId = player.Id,
                Status = BattleStatus.Completed,
                Turns = 12,
                Player1RatingChange = 22,
                Player2RatingChange = -22,
                CurrencyReward = 100,
                QueuedAt = DateTime.UtcNow.AddDays(-4),
                CompletedAt = DateTime.UtcNow.AddDays(-4),
                Team1ClassesJson = JsonSerializer.Serialize(new[] { "Warrior", "Ranger" }),
                Team2ClassesJson = JsonSerializer.Serialize(new[] { "Mage", "Assassin" })
            },
            // Loss 1
            new Battle
            {
                Id = Guid.NewGuid(),
                Player1Id = player.Id,
                Player2Id = opponent.Id,
                WinnerId = opponent.Id,
                Status = BattleStatus.Completed,
                Turns = 8,
                Player1RatingChange = -20,
                Player2RatingChange = 20,
                QueuedAt = DateTime.UtcNow.AddDays(-3),
                CompletedAt = DateTime.UtcNow.AddDays(-3),
                Team1ClassesJson = JsonSerializer.Serialize(new[] { "Warrior", "Mage" }),
                Team2ClassesJson = JsonSerializer.Serialize(new[] { "Tank", "Tank" })
            },
            // Win 3
            new Battle
            {
                Id = Guid.NewGuid(),
                Player1Id = opponent.Id,
                Player2Id = player.Id,
                WinnerId = player.Id,
                Status = BattleStatus.Completed,
                Turns = 15,
                Player1RatingChange = -18,
                Player2RatingChange = 18,
                CurrencyReward = 100,
                QueuedAt = DateTime.UtcNow.AddDays(-2),
                CompletedAt = DateTime.UtcNow.AddDays(-2),
                Team1ClassesJson = JsonSerializer.Serialize(new[] { "Healer", "Tank" }),
                Team2ClassesJson = JsonSerializer.Serialize(new[] { "Warrior", "Assassin" })
            },
            // Loss 2
            new Battle
            {
                Id = Guid.NewGuid(),
                Player1Id = opponent.Id,
                Player2Id = player.Id,
                WinnerId = opponent.Id,
                Status = BattleStatus.Completed,
                Turns = 6,
                Player1RatingChange = 15,
                Player2RatingChange = -15,
                QueuedAt = DateTime.UtcNow.AddDays(-1),
                CompletedAt = DateTime.UtcNow.AddDays(-1),
                Team1ClassesJson = JsonSerializer.Serialize(new[] { "Mage", "Mage" }),
                Team2ClassesJson = JsonSerializer.Serialize(new[] { "Warrior", "Ranger" })
            }
        };

        context.Battles.AddRange(battles);
        await context.SaveChangesAsync();

        var service = new PlayerAnalyticsService(context);

        // Act
        var result = await service.GetPlayerAnalyticsAsync(player.Id);

        // Assert
        Assert.Equal(5, result.TotalBattles);
        Assert.Equal(3, result.TotalWins);
        Assert.Equal(2, result.TotalLosses);
        Assert.Equal(60m, result.WinRate); // 3/5 = 60%
        Assert.Equal(300, result.TotalCurrencyEarned); // 3 wins × 100
    }

    [Fact]
    public async Task GetPlayerAnalyticsAsync_CalculatesStreaks_Correctly()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var player = new Player
        {
            Id = Guid.NewGuid(),
            Username = "TestPlayer",
            Email = "test@example.com",
            PasswordHash = "hash",
            Rating = 1200,
            Currency = 2000
        };
        var opponent = new Player
        {
            Id = Guid.NewGuid(),
            Username = "Opponent",
            Email = "opponent@example.com",
            PasswordHash = "hash",
            Rating = 1100,
            Currency = 1000
        };
        context.Players.AddRange(player, opponent);
        await context.SaveChangesAsync();

        // Create battle sequence: W W W L L W W (current win streak: 2, longest: 3)
        var battles = new List<Battle>
        {
            CreateBattle(player.Id, opponent.Id, player.Id, DateTime.UtcNow.AddDays(-6)), // Win
            CreateBattle(player.Id, opponent.Id, player.Id, DateTime.UtcNow.AddDays(-5)), // Win
            CreateBattle(player.Id, opponent.Id, player.Id, DateTime.UtcNow.AddDays(-4)), // Win (longest: 3)
            CreateBattle(player.Id, opponent.Id, opponent.Id, DateTime.UtcNow.AddDays(-3)), // Loss
            CreateBattle(player.Id, opponent.Id, opponent.Id, DateTime.UtcNow.AddDays(-2)), // Loss (longest loss: 2)
            CreateBattle(player.Id, opponent.Id, player.Id, DateTime.UtcNow.AddDays(-1)), // Win
            CreateBattle(player.Id, opponent.Id, player.Id, DateTime.UtcNow), // Win (current: 2)
        };

        context.Battles.AddRange(battles);
        await context.SaveChangesAsync();

        var service = new PlayerAnalyticsService(context);

        // Act
        var result = await service.GetPlayerAnalyticsAsync(player.Id);

        // Assert
        Assert.Equal(2, result.CurrentWinStreak);
        Assert.Equal(3, result.LongestWinStreak);
        Assert.Equal(0, result.CurrentLossStreak);
        Assert.Equal(2, result.LongestLossStreak);
    }

    [Fact]
    public async Task GetPlayerAnalyticsAsync_CalculatesAverageMetrics_Correctly()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var player = new Player
        {
            Id = Guid.NewGuid(),
            Username = "TestPlayer",
            Email = "test@example.com",
            PasswordHash = "hash",
            Rating = 1200,
            Currency = 2000
        };
        var opponent = new Player
        {
            Id = Guid.NewGuid(),
            Username = "Opponent",
            Email = "opponent@example.com",
            PasswordHash = "hash",
            Rating = 1100,
            Currency = 1000
        };
        context.Players.AddRange(player, opponent);
        await context.SaveChangesAsync();

        var battles = new List<Battle>
        {
            new Battle
            {
                Id = Guid.NewGuid(),
                Player1Id = player.Id,
                Player2Id = opponent.Id,
                WinnerId = player.Id,
                Status = BattleStatus.Completed,
                Turns = 10,
                Player1RatingChange = 20,
                Player2RatingChange = -20,
                CompletedAt = DateTime.UtcNow,
                Team1ClassesJson = "[]",
                Team2ClassesJson = "[]"
            },
            new Battle
            {
                Id = Guid.NewGuid(),
                Player1Id = player.Id,
                Player2Id = opponent.Id,
                WinnerId = player.Id,
                Status = BattleStatus.Completed,
                Turns = 20,
                Player1RatingChange = 30,
                Player2RatingChange = -30,
                CompletedAt = DateTime.UtcNow,
                Team1ClassesJson = "[]",
                Team2ClassesJson = "[]"
            },
            new Battle
            {
                Id = Guid.NewGuid(),
                Player1Id = player.Id,
                Player2Id = opponent.Id,
                WinnerId = opponent.Id,
                Status = BattleStatus.Completed,
                Turns = 5,
                Player1RatingChange = -10,
                Player2RatingChange = 10,
                CompletedAt = DateTime.UtcNow,
                Team1ClassesJson = "[]",
                Team2ClassesJson = "[]"
            }
        };

        context.Battles.AddRange(battles);
        await context.SaveChangesAsync();

        var service = new PlayerAnalyticsService(context);

        // Act
        var result = await service.GetPlayerAnalyticsAsync(player.Id);

        // Assert
        Assert.Equal(11.67m, Math.Round(result.AverageTurnsPerBattle, 2)); // (10+20+5)/3
        Assert.Equal(25m, result.AverageRatingChangePerWin); // (20+30)/2
        Assert.Equal(-10m, result.AverageRatingChangePerLoss); // -10/1
        Assert.Equal(30, result.HighestRatingChange);
        Assert.Equal(-10, result.LowestRatingChange);
    }

    [Fact]
    public async Task GetPlayerAnalyticsAsync_CalculatesTimeBasedStats_Correctly()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var player = new Player
        {
            Id = Guid.NewGuid(),
            Username = "TestPlayer",
            Email = "test@example.com",
            PasswordHash = "hash",
            Rating = 1200,
            Currency = 2000
        };
        var opponent = new Player
        {
            Id = Guid.NewGuid(),
            Username = "Opponent",
            Email = "opponent@example.com",
            PasswordHash = "hash",
            Rating = 1100,
            Currency = 1000
        };
        context.Players.AddRange(player, opponent);
        await context.SaveChangesAsync();

        var today = DateTime.UtcNow.Date;
        var weekStart = today.AddDays(-(int)today.DayOfWeek); // Start of this week (Sunday)

        // Use explicit times relative to today's date to avoid UTC early-morning edge cases
        var todayNoon = today.AddHours(12);
        var todayMorning = today.AddHours(9);

        // "Yesterday" for this-week test: pick a day this week that isn't today
        var thisWeekNotToday = today.DayOfWeek == DayOfWeek.Sunday
            ? weekStart.AddDays(1).AddHours(10)  // If Sunday, use Monday
            : weekStart.AddHours(10);             // Otherwise use Sunday (start of week)

        var battles = new List<Battle>
        {
            // Today - 2 battles, 1 win
            CreateBattle(player.Id, opponent.Id, player.Id, todayMorning),
            CreateBattle(player.Id, opponent.Id, opponent.Id, todayNoon),

            // This week but not today - 1 battle, 1 win
            CreateBattle(player.Id, opponent.Id, player.Id, thisWeekNotToday),

            // This month (but not this week) - 1 battle, 0 wins
            CreateBattle(player.Id, opponent.Id, opponent.Id, weekStart.AddDays(-3)),

            // Older - 1 battle, 1 win
            CreateBattle(player.Id, opponent.Id, player.Id, today.AddMonths(-2))
        };

        context.Battles.AddRange(battles);
        await context.SaveChangesAsync();

        var service = new PlayerAnalyticsService(context);

        // Act
        var result = await service.GetPlayerAnalyticsAsync(player.Id);

        // Assert
        Assert.Equal(2, result.BattlesToday);
        Assert.Equal(1, result.WinsToday);

        Assert.True(result.BattlesThisWeek >= 3, $"Expected at least 3 battles this week, got {result.BattlesThisWeek}");
        Assert.True(result.WinsThisWeek >= 2, $"Expected at least 2 wins this week, got {result.WinsThisWeek}");

        Assert.True(result.BattlesThisMonth >= 4, $"Expected at least 4 battles this month, got {result.BattlesThisMonth}");
        Assert.True(result.WinsThisMonth >= 2, $"Expected at least 2 wins this month, got {result.WinsThisMonth}");
    }

    [Fact]
    public async Task GetPlayerAnalyticsAsync_CalculatesMostUsedClasses_Correctly()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var player = new Player
        {
            Id = Guid.NewGuid(),
            Username = "TestPlayer",
            Email = "test@example.com",
            PasswordHash = "hash",
            Rating = 1200,
            Currency = 2000
        };
        var opponent = new Player
        {
            Id = Guid.NewGuid(),
            Username = "Opponent",
            Email = "opponent@example.com",
            PasswordHash = "hash",
            Rating = 1100,
            Currency = 1000
        };
        context.Players.AddRange(player, opponent);
        await context.SaveChangesAsync();

        // Warrior: 3, Mage: 2, Ranger: 1
        var battles = new List<Battle>
        {
            new Battle
            {
                Id = Guid.NewGuid(),
                Player1Id = player.Id,
                Player2Id = opponent.Id,
                WinnerId = player.Id,
                Status = BattleStatus.Completed,
                CompletedAt = DateTime.UtcNow,
                Team1ClassesJson = JsonSerializer.Serialize(new[] { "Warrior", "Mage" }),
                Team2ClassesJson = "[]"
            },
            new Battle
            {
                Id = Guid.NewGuid(),
                Player1Id = player.Id,
                Player2Id = opponent.Id,
                WinnerId = player.Id,
                Status = BattleStatus.Completed,
                CompletedAt = DateTime.UtcNow,
                Team1ClassesJson = JsonSerializer.Serialize(new[] { "Warrior", "Mage" }),
                Team2ClassesJson = "[]"
            },
            new Battle
            {
                Id = Guid.NewGuid(),
                Player1Id = player.Id,
                Player2Id = opponent.Id,
                WinnerId = player.Id,
                Status = BattleStatus.Completed,
                CompletedAt = DateTime.UtcNow,
                Team1ClassesJson = JsonSerializer.Serialize(new[] { "Warrior", "Ranger" }),
                Team2ClassesJson = "[]"
            }
        };

        context.Battles.AddRange(battles);
        await context.SaveChangesAsync();

        var service = new PlayerAnalyticsService(context);

        // Act
        var result = await service.GetPlayerAnalyticsAsync(player.Id);

        // Assert
        Assert.Equal(3, result.MostUsedClasses.Count);
        Assert.Equal("Warrior", result.MostUsedClasses[0].ClassName);
        Assert.Equal(3, result.MostUsedClasses[0].TimesUsed);
        Assert.Equal(50m, result.MostUsedClasses[0].Percentage); // 3/6 total class slots

        Assert.Equal("Mage", result.MostUsedClasses[1].ClassName);
        Assert.Equal(2, result.MostUsedClasses[1].TimesUsed);
    }

    [Fact]
    public async Task GetPlayerAnalyticsAsync_CalculatesWinRateByClass_Correctly()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var player = new Player
        {
            Id = Guid.NewGuid(),
            Username = "TestPlayer",
            Email = "test@example.com",
            PasswordHash = "hash",
            Rating = 1200,
            Currency = 2000
        };
        var opponent = new Player
        {
            Id = Guid.NewGuid(),
            Username = "Opponent",
            Email = "opponent@example.com",
            PasswordHash = "hash",
            Rating = 1100,
            Currency = 1000
        };
        context.Players.AddRange(player, opponent);
        await context.SaveChangesAsync();

        // Warrior: 2 wins, 1 loss (66.7%)
        // Mage: 1 win, 1 loss (50%)
        var battles = new List<Battle>
        {
            new Battle
            {
                Id = Guid.NewGuid(),
                Player1Id = player.Id,
                Player2Id = opponent.Id,
                WinnerId = player.Id,
                Status = BattleStatus.Completed,
                CompletedAt = DateTime.UtcNow,
                Team1ClassesJson = JsonSerializer.Serialize(new[] { "Warrior", "Mage" }),
                Team2ClassesJson = "[]"
            },
            new Battle
            {
                Id = Guid.NewGuid(),
                Player1Id = player.Id,
                Player2Id = opponent.Id,
                WinnerId = player.Id,
                Status = BattleStatus.Completed,
                CompletedAt = DateTime.UtcNow,
                Team1ClassesJson = JsonSerializer.Serialize(new[] { "Warrior" }),
                Team2ClassesJson = "[]"
            },
            new Battle
            {
                Id = Guid.NewGuid(),
                Player1Id = player.Id,
                Player2Id = opponent.Id,
                WinnerId = opponent.Id,
                Status = BattleStatus.Completed,
                CompletedAt = DateTime.UtcNow,
                Team1ClassesJson = JsonSerializer.Serialize(new[] { "Warrior", "Mage" }),
                Team2ClassesJson = "[]"
            }
        };

        context.Battles.AddRange(battles);
        await context.SaveChangesAsync();

        var service = new PlayerAnalyticsService(context);

        // Act
        var result = await service.GetPlayerAnalyticsAsync(player.Id);

        // Assert
        Assert.Equal(2, result.WinRateByClass.Count);

        var warriorStats = result.WinRateByClass.First(c => c.ClassName == "Warrior");
        Assert.Equal(2, warriorStats.Wins);
        Assert.Equal(1, warriorStats.Losses);
        Assert.Equal(3, warriorStats.TotalBattles);
        Assert.Equal(66.67m, Math.Round(warriorStats.WinRate, 2));

        var mageStats = result.WinRateByClass.First(c => c.ClassName == "Mage");
        Assert.Equal(1, mageStats.Wins);
        Assert.Equal(1, mageStats.Losses);
        Assert.Equal(50m, mageStats.WinRate);
    }

    [Fact]
    public async Task GetPlayerAnalyticsAsync_ReturnsRecentBattles_InCorrectOrder()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var player = new Player
        {
            Id = Guid.NewGuid(),
            Username = "TestPlayer",
            Email = "test@example.com",
            PasswordHash = "hash",
            Rating = 1200,
            Currency = 2000
        };
        var opponent = new Player
        {
            Id = Guid.NewGuid(),
            Username = "Opponent",
            Email = "opponent@example.com",
            PasswordHash = "hash",
            Rating = 1100,
            Currency = 1000
        };
        context.Players.AddRange(player, opponent);
        await context.SaveChangesAsync();

        var battles = new List<Battle>
        {
            CreateBattle(player.Id, opponent.Id, player.Id, DateTime.UtcNow.AddDays(-3)),
            CreateBattle(player.Id, opponent.Id, opponent.Id, DateTime.UtcNow.AddDays(-2)),
            CreateBattle(player.Id, opponent.Id, player.Id, DateTime.UtcNow.AddDays(-1))
        };

        context.Battles.AddRange(battles);
        await context.SaveChangesAsync();

        var service = new PlayerAnalyticsService(context);

        // Act
        var result = await service.GetPlayerAnalyticsAsync(player.Id);

        // Assert
        Assert.Equal(3, result.RecentBattles.Count);

        // Should be ordered by most recent first
        Assert.True(result.RecentBattles[0].CompletedAt > result.RecentBattles[1].CompletedAt);
        Assert.True(result.RecentBattles[1].CompletedAt > result.RecentBattles[2].CompletedAt);

        // Verify win/loss data
        Assert.True(result.RecentBattles[0].IsWin); // Most recent
        Assert.False(result.RecentBattles[1].IsWin);
        Assert.True(result.RecentBattles[2].IsWin);
    }

    [Fact]
    public async Task GetPlayerAnalyticsAsync_CalculatesPeakPerformance_Correctly()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var player = new Player
        {
            Id = Guid.NewGuid(),
            Username = "TestPlayer",
            Email = "test@example.com",
            PasswordHash = "hash",
            Rating = 1200,
            Currency = 2000
        };
        var opponent = new Player
        {
            Id = Guid.NewGuid(),
            Username = "Opponent",
            Email = "opponent@example.com",
            PasswordHash = "hash",
            Rating = 1100,
            Currency = 1000
        };
        context.Players.AddRange(player, opponent);
        await context.SaveChangesAsync();

        var baseDate = DateTime.UtcNow.Date;
        var battles = new List<Battle>
        {
            // Day 1: 1 battle, 1 win
            CreateBattle(player.Id, opponent.Id, player.Id, baseDate.AddDays(-3).AddHours(10)),

            // Day 2: 5 battles, 3 wins (most active day, best day)
            CreateBattle(player.Id, opponent.Id, player.Id, baseDate.AddDays(-2).AddHours(8)),
            CreateBattle(player.Id, opponent.Id, player.Id, baseDate.AddDays(-2).AddHours(10)),
            CreateBattle(player.Id, opponent.Id, opponent.Id, baseDate.AddDays(-2).AddHours(12)),
            CreateBattle(player.Id, opponent.Id, player.Id, baseDate.AddDays(-2).AddHours(14)),
            CreateBattle(player.Id, opponent.Id, opponent.Id, baseDate.AddDays(-2).AddHours(16)),

            // Day 3: 2 battles, 1 win
            CreateBattle(player.Id, opponent.Id, player.Id, baseDate.AddDays(-1).AddHours(10)),
            CreateBattle(player.Id, opponent.Id, opponent.Id, baseDate.AddDays(-1).AddHours(12))
        };

        context.Battles.AddRange(battles);
        await context.SaveChangesAsync();

        var service = new PlayerAnalyticsService(context);

        // Act
        var result = await service.GetPlayerAnalyticsAsync(player.Id);

        // Assert
        Assert.NotNull(result.BestDayDate);
        Assert.Equal(3, result.BestDayWins);

        Assert.NotNull(result.MostActiveDayDate);
        Assert.Equal(5, result.MostActiveDayBattles);

        // Both should be the same day in this case
        Assert.Equal(result.BestDayDate!.Value.Date, result.MostActiveDayDate!.Value.Date);
    }

    // Helper method
    private Battle CreateBattle(Guid player1Id, Guid player2Id, Guid winnerId, DateTime completedAt)
    {
        return new Battle
        {
            Id = Guid.NewGuid(),
            Player1Id = player1Id,
            Player2Id = player2Id,
            WinnerId = winnerId,
            Status = BattleStatus.Completed,
            Turns = 10,
            Player1RatingChange = winnerId == player1Id ? 20 : -20,
            Player2RatingChange = winnerId == player2Id ? 20 : -20,
            CurrencyReward = winnerId == player1Id || winnerId == player2Id ? 100 : 0,
            QueuedAt = completedAt.AddMinutes(-5),
            CompletedAt = completedAt,
            Team1ClassesJson = "[]",
            Team2ClassesJson = "[]"
        };
    }
}
