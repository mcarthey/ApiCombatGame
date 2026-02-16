using ApiCombatGame.Models.Domain;
using ApiCombatGame.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ApiCombatGame.Data;

/// <summary>
/// Seeds AI bot players across different rating tiers to ensure matchmaking availability.
/// </summary>
public static class BotSeedData
{
    /// <summary>
    /// Initializes bot players if they don't exist.
    /// Creates bots distributed across rating tiers from beginner to expert.
    /// </summary>
    public static async Task InitializeAsync(GameDbContext context, IBotNameGenerator nameGenerator)
    {
        // Check if bots already exist
        var existingBotCount = await context.Players.CountAsync(p => p.IsBot);
        if (existingBotCount > 0)
            return; // Bots already seeded

        // Rating distribution: create bots across the rating spectrum
        // This ensures players at any rating can find a match
        var botConfigs = new List<(int rating, int count)>
        {
            (900, 5),    // Beginner bots (below starting rating)
            (1000, 10),  // Starting rating bots (most common)
            (1100, 8),   // Slightly above average
            (1200, 6),   // Intermediate
            (1300, 5),   // Advanced
            (1400, 4),   // Expert
            (1500, 3),   // Master
            (1600, 2),   // Elite
            (1700, 1),   // Champion
            (1800, 1)    // Legend
        };

        var bots = new List<Player>();

        foreach (var (rating, count) in botConfigs)
        {
            for (int i = 0; i < count; i++)
            {
                var name = nameGenerator.GenerateBotName();
                var bot = new Player
                {
                    Id = Guid.NewGuid(),
                    Username = name,
                    Email = $"{name.ToLower().Replace("_", "")}@bots.apicombat.internal",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()), // Random unusable password
                    Level = CalculateLevelFromRating(rating),
                    Currency = 1000,
                    Rating = rating + Random.Shared.Next(-20, 21), // Add slight variance
                    IsBot = true,
                    IsAdmin = false,
                    CreatedAt = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 90)), // Bots "joined" over last 90 days
                    LastLoginAt = DateTime.UtcNow.AddHours(-Random.Shared.Next(1, 48)), // Last "active" within 48 hours
                    LastBattleResetDate = DateTime.UtcNow.Date
                };

                bots.Add(bot);
            }
        }

        context.Players.AddRange(bots);
        await context.SaveChangesAsync();

        Console.WriteLine($"Seeded {bots.Count} bot players across rating tiers 900-1800");
    }

    /// <summary>
    /// Approximates player level based on rating.
    /// Higher rating bots appear more experienced.
    /// </summary>
    private static int CalculateLevelFromRating(int rating)
    {
        return rating switch
        {
            < 1000 => 1,
            < 1100 => Random.Shared.Next(1, 3),
            < 1200 => Random.Shared.Next(2, 5),
            < 1300 => Random.Shared.Next(4, 7),
            < 1400 => Random.Shared.Next(6, 10),
            < 1500 => Random.Shared.Next(8, 12),
            < 1600 => Random.Shared.Next(10, 15),
            < 1700 => Random.Shared.Next(12, 18),
            < 1800 => Random.Shared.Next(15, 20),
            _ => Random.Shared.Next(18, 25)
        };
    }
}
