using System.Text.Json;
using ApiCombatGame.Data;
using ApiCombatGame.Models.Domain;
using ApiCombatGame.Models.DTOs.Strategy;
using ApiCombatGame.Models.Enums;
using ApiCombatGame.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ApiCombatGame.Services;

/// <summary>
/// Generates team compositions and strategies for AI bot players.
/// Team quality scales with bot rating to provide appropriate challenge.
/// </summary>
public class BotTeamGenerator : IBotTeamGenerator
{
    private readonly GameDbContext _context;
    private readonly ILogger<BotTeamGenerator> _logger;

    public BotTeamGenerator(GameDbContext context, ILogger<BotTeamGenerator> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Team> CreateBotTeamAsync(Player bot, int teamNumber = 1)
    {
        if (!bot.IsBot)
            throw new InvalidOperationException("Cannot create bot team for non-bot player");

        // Get all template units
        var templateUnits = await _context.Units
            .Where(u => u.IsTemplate)
            .ToListAsync();

        if (templateUnits.Count == 0)
            throw new InvalidOperationException("No template units available for bot teams");

        // Select units based on bot rating
        var selectedUnits = SelectUnitsForRating(templateUnits, bot.Rating);

        // Generate strategy based on rating
        var strategy = GenerateStrategyForRating(bot.Rating, selectedUnits);

        // Create team
        var team = new Team
        {
            Id = Guid.NewGuid(),
            Name = $"{bot.Username} Squad {teamNumber}",
            PlayerId = bot.Id,
            UnitIdsJson = JsonSerializer.Serialize(selectedUnits.Select(u => u.Id).ToList()),
            StrategyJson = JsonSerializer.Serialize(strategy),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Teams.Add(team);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created team for bot {BotName} (rating: {Rating}) with {UnitCount} units",
            bot.Username, bot.Rating, selectedUnits.Count);

        return team;
    }

    public async Task EnsureBotHasTeamAsync(Player bot)
    {
        if (!bot.IsBot)
            return;

        var existingTeam = await _context.Teams.FirstOrDefaultAsync(t => t.PlayerId == bot.Id);
        if (existingTeam == null)
        {
            await CreateBotTeamAsync(bot);
        }
    }

    /// <summary>
    /// Selects units appropriate for the bot's rating tier.
    /// Lower rating = basic units, higher rating = more diverse and stronger compositions.
    /// </summary>
    private List<Unit> SelectUnitsForRating(List<Unit> availableUnits, int rating)
    {
        var teamSize = 5;
        var selectedUnits = new List<Unit>();

        // Categorize units by class for balanced team composition
        var warriors = availableUnits.Where(u => u.Class == UnitClass.Warrior).ToList();
        var mages = availableUnits.Where(u => u.Class == UnitClass.Mage).ToList();
        var healers = availableUnits.Where(u => u.Class == UnitClass.Healer).ToList();
        var rangers = availableUnits.Where(u => u.Class == UnitClass.Ranger).ToList();
        var tanks = availableUnits.Where(u => u.Class == UnitClass.Tank).ToList();

        // Rating-based team composition strategy
        if (rating < 1000)
        {
            // Beginner bots: Simple composition, basic units
            selectedUnits.Add(PickRandom(warriors, 1).FirstOrDefault() ?? PickRandom(availableUnits, 1).First());
            selectedUnits.Add(PickRandom(mages, 1).FirstOrDefault() ?? PickRandom(availableUnits, 1).First());
            selectedUnits.Add(PickRandom(warriors, 1).FirstOrDefault() ?? PickRandom(availableUnits, 1).First());
            selectedUnits.Add(PickRandom(healers, 1).FirstOrDefault() ?? PickRandom(availableUnits, 1).First());
            selectedUnits.Add(PickRandom(tanks, 1).FirstOrDefault() ?? PickRandom(availableUnits, 1).First());
        }
        else if (rating < 1200)
        {
            // Intermediate bots: Balanced composition
            selectedUnits.AddRange(PickRandom(warriors, 1));
            selectedUnits.AddRange(PickRandom(mages, 1));
            selectedUnits.AddRange(PickRandom(healers, 1));
            selectedUnits.AddRange(PickRandom(rangers.Concat(tanks).ToList(), 2));
        }
        else if (rating < 1400)
        {
            // Advanced bots: Strategic diversity
            var allClasses = new[] { warriors, mages, healers, rangers, tanks };
            var nonEmptyClasses = allClasses.Where(c => c.Count > 0).ToList();

            foreach (var classGroup in nonEmptyClasses.Take(5))
            {
                selectedUnits.AddRange(PickRandom(classGroup, 1));
            }
        }
        else
        {
            // Expert bots: Optimized meta compositions
            // Mix of strong frontline, backline damage, and support
            selectedUnits.AddRange(PickRandom(warriors, 2));
            selectedUnits.AddRange(PickRandom(mages, 1));
            selectedUnits.AddRange(PickRandom(healers, 1));
            selectedUnits.AddRange(PickRandom(tanks.Concat(rangers).ToList(), 1));
        }

        // Ensure we have exactly teamSize units
        while (selectedUnits.Count < teamSize && availableUnits.Count > 0)
        {
            var remaining = availableUnits.Except(selectedUnits).ToList();
            if (remaining.Count > 0)
                selectedUnits.Add(PickRandom(remaining, 1).First());
            else
                break;
        }

        return selectedUnits.Take(teamSize).ToList();
    }

    /// <summary>
    /// Generates a battle strategy appropriate for the bot's rating tier.
    /// Lower rating = simple strategies, higher rating = more sophisticated tactics.
    /// </summary>
    private StrategyConfig GenerateStrategyForRating(int rating, List<Unit> teamUnits)
    {
        var strategy = new StrategyConfig();

        // Formation selection based on rating
        if (rating < 1000)
        {
            strategy.Formation = "balanced";
            strategy.TargetPriority = new List<string> { "lowest_hp" };
        }
        else if (rating < 1200)
        {
            var formations = new[] { "aggressive", "defensive", "balanced" };
            strategy.Formation = formations[Random.Shared.Next(formations.Length)];
            strategy.TargetPriority = new List<string> { "lowest_hp", "healers" };
        }
        else if (rating < 1400)
        {
            var formations = new[] { "aggressive", "defensive" };
            strategy.Formation = formations[Random.Shared.Next(formations.Length)];
            strategy.TargetPriority = new List<string> { "highest_threat", "healers", "lowest_hp" };
        }
        else
        {
            // Expert bots use sophisticated strategies
            strategy.Formation = rating > 1600 ? "aggressive" : "defensive";
            strategy.TargetPriority = new List<string> { "healers", "highest_threat", "lowest_hp" };

            // Add ability conditions for higher-rated bots
            // (In a full implementation, this would be based on actual unit abilities)
            strategy.Abilities = new Dictionary<string, AbilityCondition>
            {
                ["Heal"] = new AbilityCondition
                {
                    When = "ally_hp_below_50",
                    Target = "lowest_ally_hp"
                },
                ["Fireball"] = new AbilityCondition
                {
                    When = "enemy_count_gte_2",
                    Target = "priority"
                },
                ["Shield"] = new AbilityCondition
                {
                    When = "ally_hp_below_25",
                    Target = "lowest_ally_hp"
                }
            };
        }

        return strategy;
    }

    /// <summary>
    /// Picks N random items from a list.
    /// </summary>
    private List<T> PickRandom<T>(List<T> items, int count)
    {
        if (items.Count == 0) return new List<T>();
        return items.OrderBy(_ => Random.Shared.Next()).Take(count).ToList();
    }
}
