using ApiCombatGame.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace ApiCombatGame.Data;

public static class Phase3SeedData
{
    public static async Task InitializeAsync(GameDbContext context)
    {
        await SeedModifiers(context);
        await SeedAchievements(context);
        await SeedPlayerTitles(context);
    }

    private static async Task SeedModifiers(GameDbContext context)
    {
        if (await context.EnvironmentalModifiers.AnyAsync())
            return;

        var now = DateTime.UtcNow;
        // Calculate start of current week (Monday)
        var daysFromMonday = ((int)now.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        var weekStart = now.Date.AddDays(-daysFromMonday);

        var modifiers = new List<EnvironmentalModifier>
        {
            new EnvironmentalModifier
            {
                Id = Guid.NewGuid(),
                Name = "Arcane Disruption",
                Description = "Mage abilities are weakened, physical attacks deal +20% damage. Favors Warriors and Rangers.",
                ModifierType = "ArcaneDisruption",
                ConfigJson = "{\"mageAttackMultiplier\":0.7,\"physicalAttackMultiplier\":1.2}",
                StartDate = weekStart,
                EndDate = weekStart.AddDays(7),
                IsActive = true,
                CreatedAt = now
            },
            new EnvironmentalModifier
            {
                Id = Guid.NewGuid(),
                Name = "Heavy Armor",
                Description = "All units gain +50% defense, healer abilities have 2x effectiveness. Favors defensive comps.",
                ModifierType = "HeavyArmor",
                ConfigJson = "{\"defenseMultiplier\":1.5,\"healingMultiplier\":2.0}",
                StartDate = weekStart.AddDays(7),
                EndDate = weekStart.AddDays(14),
                IsActive = false,
                CreatedAt = now
            }
        };

        context.EnvironmentalModifiers.AddRange(modifiers);
        await context.SaveChangesAsync();
    }

    private static async Task SeedAchievements(GameDbContext context)
    {
        if (await context.Achievements.AnyAsync())
            return;

        var achievements = new List<Achievement>
        {
            // Combat achievements
            new Achievement
            {
                Id = Guid.NewGuid(),
                Name = "First Blood",
                Description = "Win your first battle",
                Category = "Combat",
                Points = 10,
                RequirementsJson = "{\"type\":\"wins\",\"count\":1}",
                CreatedAt = DateTime.UtcNow
            },
            new Achievement
            {
                Id = Guid.NewGuid(),
                Name = "Veteran",
                Description = "Win 50 battles",
                Category = "Combat",
                Points = 50,
                RequirementsJson = "{\"type\":\"wins\",\"count\":50}",
                CreatedAt = DateTime.UtcNow
            },
            new Achievement
            {
                Id = Guid.NewGuid(),
                Name = "Unbreakable",
                Description = "Win 10 battles in a row",
                Category = "Combat",
                Points = 100,
                RequirementsJson = "{\"type\":\"winStreak\",\"count\":10}",
                CreatedAt = DateTime.UtcNow
            },

            // Collection achievements
            new Achievement
            {
                Id = Guid.NewGuid(),
                Name = "Collector",
                Description = "Unlock 10 different units",
                Category = "Collection",
                Points = 25,
                RequirementsJson = "{\"type\":\"unitsOwned\",\"count\":10}",
                CreatedAt = DateTime.UtcNow
            },
            new Achievement
            {
                Id = Guid.NewGuid(),
                Name = "Master Trainer",
                Description = "Reach mastery level 5 with any unit",
                Category = "Collection",
                Points = 75,
                RequirementsJson = "{\"type\":\"masteryLevel\",\"level\":5}",
                CreatedAt = DateTime.UtcNow
            },

            // Social achievements
            new Achievement
            {
                Id = Guid.NewGuid(),
                Name = "Team Player",
                Description = "Join a guild",
                Category = "Social",
                Points = 15,
                RequirementsJson = "{\"type\":\"joinGuild\"}",
                CreatedAt = DateTime.UtcNow
            },
            new Achievement
            {
                Id = Guid.NewGuid(),
                Name = "Dragon Slayer",
                Description = "Deal the killing blow to a guild boss",
                Category = "Social",
                Points = 150,
                RequirementsJson = "{\"type\":\"bossKillingBlow\"}",
                IsSecret = true,
                CreatedAt = DateTime.UtcNow
            },

            // Strategy achievements
            new Achievement
            {
                Id = Guid.NewGuid(),
                Name = "Strategist",
                Description = "Upload a strategy to the marketplace",
                Category = "Strategy",
                Points = 20,
                RequirementsJson = "{\"type\":\"uploadStrategy\"}",
                CreatedAt = DateTime.UtcNow
            },
            new Achievement
            {
                Id = Guid.NewGuid(),
                Name = "Popular Tactician",
                Description = "Have a strategy downloaded 100 times",
                Category = "Strategy",
                Points = 100,
                RequirementsJson = "{\"type\":\"strategyDownloads\",\"count\":100}",
                CreatedAt = DateTime.UtcNow
            }
        };

        context.Achievements.AddRange(achievements);
        await context.SaveChangesAsync();
    }

    private static async Task SeedPlayerTitles(GameDbContext context)
    {
        if (await context.PlayerTitles.AnyAsync())
            return;

        var titles = new List<PlayerTitle>
        {
            new PlayerTitle
            {
                Id = Guid.NewGuid(),
                Name = "Recruit",
                Description = "Starting title for all players",
                ColorHex = "#9CA3AF",
                UnlockRequirementsJson = "{\"type\":\"default\"}",
                CreatedAt = DateTime.UtcNow
            },
            new PlayerTitle
            {
                Id = Guid.NewGuid(),
                Name = "Warrior",
                Description = "Reach 1200 rating",
                ColorHex = "#10B981",
                UnlockRequirementsJson = "{\"type\":\"rating\",\"minRating\":1200}",
                CreatedAt = DateTime.UtcNow
            },
            new PlayerTitle
            {
                Id = Guid.NewGuid(),
                Name = "Champion",
                Description = "Reach 1500 rating",
                ColorHex = "#3B82F6",
                UnlockRequirementsJson = "{\"type\":\"rating\",\"minRating\":1500}",
                CreatedAt = DateTime.UtcNow
            },
            new PlayerTitle
            {
                Id = Guid.NewGuid(),
                Name = "Grand Master",
                Description = "Reach 1800 rating",
                ColorHex = "#8B5CF6",
                UnlockRequirementsJson = "{\"type\":\"rating\",\"minRating\":1800}",
                CreatedAt = DateTime.UtcNow
            },
            new PlayerTitle
            {
                Id = Guid.NewGuid(),
                Name = "Legend",
                Description = "Reach 2000 rating",
                ColorHex = "#FFD700",
                UnlockRequirementsJson = "{\"type\":\"rating\",\"minRating\":2000}",
                CreatedAt = DateTime.UtcNow
            },
            new PlayerTitle
            {
                Id = Guid.NewGuid(),
                Name = "Guild Leader",
                Description = "Create and lead a guild",
                ColorHex = "#F59E0B",
                UnlockRequirementsJson = "{\"type\":\"guildLeader\"}",
                CreatedAt = DateTime.UtcNow
            }
        };

        context.PlayerTitles.AddRange(titles);
        await context.SaveChangesAsync();
    }
}
