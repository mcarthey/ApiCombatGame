using ApiCombatGame.Data;
using ApiCombatGame.Models.Domain;
using ApiCombatGame.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ApiCombatGame.Services;

public class EasterEggService : IEasterEggService
{
    private readonly GameDbContext _context;
    private readonly ILogger<EasterEggService> _logger;

    // Pages where eggs can be hidden
    private static readonly string[] EggPages =
    {
        "/", "/api-docs/v1", "/rules", "/leaderboard",
        "/Dashboard", "/Account", "/Account/Subscription", "/Account/ApiKeys"
    };

    // Possible CSS selectors for egg placement (areas of the page)
    private static readonly Dictionary<string, string[]> PageSelectors = new()
    {
        ["/"] = new[] { ".hero-section", ".features-section", "footer" },
        ["/api-docs/v1"] = new[] { "#player", "#battle", "#team", "#unit", "#leaderboard" },
        ["/rules"] = new[] { ".rules-content", ".rules-sidebar" },
        ["/leaderboard"] = new[] { ".leaderboard-table", ".leaderboard-header" },
        ["/Dashboard"] = new[] { ".dash-panel-secondary", ".md3-card" },
        ["/Account"] = new[] { ".acct-nav-card", ".md3-card" },
        ["/Account/Subscription"] = new[] { ".md3-card", ".md3-card-elevated" },
        ["/Account/ApiKeys"] = new[] { ".md3-card" },
    };

    // Loot tables by difficulty
    private static readonly (string Type, string Desc, int Min, int Max)[] EasyLoot =
    {
        ("Currency", "Gold", 100, 300),
        ("Currency", "Gold", 200, 500),
        ("Gems", "Gems", 5, 15),
    };

    private static readonly (string Type, string Desc, int Min, int Max)[] MediumLoot =
    {
        ("Currency", "Gold", 400, 800),
        ("Gems", "Gems", 15, 30),
        ("XpBoost", "XP Boost", 500, 1000),
    };

    private static readonly (string Type, string Desc, int Min, int Max)[] HardLoot =
    {
        ("Currency", "Gold", 800, 1500),
        ("Gems", "Gems", 30, 60),
        ("XpBoost", "XP Boost", 1000, 2000),
    };

    public EasterEggService(GameDbContext context, ILogger<EasterEggService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<EasterEgg?> GetActiveEggForPageAsync(string pagePath)
    {
        var now = DateTime.UtcNow;
        return await _context.EasterEggs
            .FirstOrDefaultAsync(e =>
                e.PagePath == pagePath &&
                e.IsActive &&
                e.WeekStart <= now &&
                e.WeekEnd >= now);
    }

    public async Task<bool> HasPlayerClaimedAsync(Guid playerId, Guid eggId)
    {
        return await _context.EggClaims
            .AnyAsync(c => c.PlayerId == playerId && c.EasterEggId == eggId);
    }

    public async Task<EggClaim?> RedeemCodeAsync(Guid playerId, string code)
    {
        var now = DateTime.UtcNow;

        var egg = await _context.EasterEggs
            .FirstOrDefaultAsync(e =>
                e.Code == code &&
                e.IsActive &&
                e.WeekStart <= now &&
                e.WeekEnd >= now);

        if (egg == null)
        {
            _logger.LogWarning("Invalid or expired egg code '{Code}' attempted by player {PlayerId}", code, playerId);
            return null;
        }

        // Check if already claimed
        var alreadyClaimed = await _context.EggClaims
            .AnyAsync(c => c.PlayerId == playerId && c.EasterEggId == egg.Id);

        if (alreadyClaimed)
        {
            _logger.LogInformation("Player {PlayerId} tried to re-claim egg {EggId}", playerId, egg.Id);
            return null;
        }

        // Roll loot
        var (lootType, lootDesc, lootAmount) = RollLoot(egg.Difficulty);

        var claim = new EggClaim
        {
            Id = Guid.NewGuid(),
            PlayerId = playerId,
            EasterEggId = egg.Id,
            LootType = lootType,
            LootDescription = $"{lootAmount} {lootDesc}",
            LootAmount = lootAmount,
            ClaimedAt = DateTime.UtcNow
        };

        _context.EggClaims.Add(claim);

        // Award the loot to the player
        var player = await _context.Players.FindAsync(playerId);
        if (player != null)
        {
            switch (lootType)
            {
                case "Currency":
                    player.Currency += lootAmount;
                    player.TotalGoldEarned += lootAmount;
                    break;
                case "Gems":
                    player.Gems += lootAmount;
                    player.GemsEarnedTotal += lootAmount;
                    break;
                case "XpBoost":
                    player.ExperiencePoints += lootAmount;
                    player.TotalXpEarned += lootAmount;
                    break;
            }
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Player {PlayerId} claimed egg {EggId}: {LootType} x{Amount}",
            playerId, egg.Id, lootType, lootAmount);

        return claim;
    }

    public async Task<List<EggClaim>> GetPlayerClaimsAsync(Guid playerId)
    {
        return await _context.EggClaims
            .Include(c => c.EasterEgg)
            .Where(c => c.PlayerId == playerId)
            .OrderByDescending(c => c.ClaimedAt)
            .ToListAsync();
    }

    public async Task<EggHuntStats> GetWeeklyStatsAsync(Guid playerId)
    {
        var now = DateTime.UtcNow;
        var weekStart = GetMondayOfWeek(now);
        var weekEnd = weekStart.AddDays(7).AddSeconds(-1);

        var totalEggs = await _context.EasterEggs
            .CountAsync(e => e.IsActive && e.WeekStart >= weekStart && e.WeekEnd <= weekEnd.AddSeconds(1));

        var playerClaims = await _context.EggClaims
            .Include(c => c.EasterEgg)
            .Where(c => c.PlayerId == playerId)
            .ToListAsync();

        var thisWeekClaims = playerClaims
            .Where(c => c.EasterEgg.WeekStart >= weekStart && c.EasterEgg.WeekEnd <= weekEnd.AddSeconds(1))
            .ToList();

        return new EggHuntStats
        {
            TotalEggsThisWeek = totalEggs,
            EggsFoundThisWeek = thisWeekClaims.Count,
            TotalLootEarned = thisWeekClaims.Sum(c => c.LootAmount),
            AllTimeEggsFound = playerClaims.Count
        };
    }

    public async Task<List<EasterEgg>> GenerateWeeklyEggsAsync(DateTime weekStart)
    {
        var monday = GetMondayOfWeek(weekStart);
        var sunday = monday.AddDays(7).AddSeconds(-1);

        // Check if eggs already exist for this week
        var existingCount = await _context.EasterEggs
            .CountAsync(e => e.WeekStart == monday);

        if (existingCount > 0)
        {
            _logger.LogInformation("Eggs already exist for week starting {WeekStart}", monday);
            return await _context.EasterEggs.Where(e => e.WeekStart == monday).ToListAsync();
        }

        var rng = new Random(monday.GetHashCode()); // Deterministic seed per week
        var eggs = new List<EasterEgg>();

        // Pick 3-5 random pages
        var shuffledPages = EggPages.OrderBy(_ => rng.Next()).Take(rng.Next(3, 6)).ToList();

        foreach (var page in shuffledPages)
        {
            var selectors = PageSelectors.GetValueOrDefault(page, new[] { "body" });
            var selector = selectors[rng.Next(selectors.Length)];

            var egg = new EasterEgg
            {
                Id = Guid.NewGuid(),
                Code = GenerateCode(rng),
                PagePath = page,
                CssSelector = selector,
                OffsetX = Math.Round(10 + rng.NextDouble() * 80, 1), // 10-90%
                OffsetY = Math.Round(10 + rng.NextDouble() * 80, 1),
                WeekStart = monday,
                WeekEnd = sunday,
                Difficulty = DifficultyForPage(page),
                IconName = rng.Next(3) switch { 0 => "egg_alt", 1 => "star", _ => "diamond" },
                CreatedAt = DateTime.UtcNow
            };

            eggs.Add(egg);
        }

        _context.EasterEggs.AddRange(eggs);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Generated {Count} easter eggs for week starting {WeekStart}", eggs.Count, monday);
        return eggs;
    }

    private static (string Type, string Desc, int Amount) RollLoot(int difficulty)
    {
        var rng = new Random();
        var table = difficulty switch
        {
            1 => EasyLoot,
            2 => MediumLoot,
            _ => HardLoot
        };

        var entry = table[rng.Next(table.Length)];
        var amount = rng.Next(entry.Min, entry.Max + 1);
        return (entry.Type, entry.Desc, amount);
    }

    private static string GenerateCode(Random rng)
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // No ambiguous chars (0/O, 1/I/L)
        var suffix = new string(Enumerable.Range(0, 4).Select(_ => chars[rng.Next(chars.Length)]).ToArray());
        return $"CHEST-{suffix}";
    }

    private static int DifficultyForPage(string page) => page switch
    {
        "/" or "/Dashboard" => 1,
        "/leaderboard" or "/Account" => 2,
        "/api-docs/v1" or "/rules" => 3,
        _ => 1
    };

    private static DateTime GetMondayOfWeek(DateTime date)
    {
        var diff = ((int)date.DayOfWeek - 1 + 7) % 7;
        return date.Date.AddDays(-diff);
    }
}
