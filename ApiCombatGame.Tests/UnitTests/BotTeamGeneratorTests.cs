using System.Text.Json;
using ApiCombatGame.Data;
using ApiCombatGame.Models.Domain;
using ApiCombatGame.Models.Enums;
using ApiCombatGame.Services;
using ApiCombatGame.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ApiCombatGame.Tests.UnitTests;

public class BotTeamGeneratorTests : IDisposable
{
    private readonly GameDbContext _context;
    private readonly Mock<ILogger<BotTeamGenerator>> _logger;
    private readonly BotTeamGenerator _service;

    public BotTeamGeneratorTests()
    {
        _context = TestDbContextFactory.Create();
        _logger = new Mock<ILogger<BotTeamGenerator>>();
        _service = new BotTeamGenerator(_context, _logger.Object);

        SeedTemplateUnits();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    private void SeedTemplateUnits()
    {
        var classes = new[]
        {
            (UnitClass.Warrior, "Iron Knight", 120, 25, 30, 10),
            (UnitClass.Warrior, "Berserker", 100, 35, 15, 12),
            (UnitClass.Mage, "Fire Mage", 80, 40, 8, 14),
            (UnitClass.Mage, "Ice Mage", 85, 38, 10, 13),
            (UnitClass.Healer, "Priest", 90, 10, 15, 16),
            (UnitClass.Healer, "Druid", 95, 12, 14, 15),
            (UnitClass.Ranger, "Scout", 85, 30, 12, 18),
            (UnitClass.Ranger, "Sniper", 75, 35, 8, 20),
            (UnitClass.Tank, "Guardian", 150, 15, 40, 6),
            (UnitClass.Tank, "Paladin", 140, 18, 35, 8)
        };

        foreach (var (cls, name, hp, atk, def, spd) in classes)
        {
            _context.Units.Add(new Unit
            {
                Id = Guid.NewGuid(),
                Name = name,
                Class = cls,
                Health = hp,
                Attack = atk,
                Defense = def,
                Speed = spd,
                IsTemplate = true,
                UnlockCost = 200
            });
        }
        _context.SaveChanges();
    }

    private Player CreateBot(string name = "TestBot", int rating = 1000)
    {
        var bot = new Player
        {
            Id = Guid.NewGuid(),
            Username = name,
            Email = $"{name}@bot.test",
            PasswordHash = "botpass",
            IsBot = true,
            Rating = rating,
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow
        };
        _context.Players.Add(bot);
        _context.SaveChanges();
        return bot;
    }

    [Fact]
    public async Task CreateBotTeam_SelectsUnitsFromTemplates()
    {
        var bot = CreateBot(rating: 1100);

        var team = await _service.CreateBotTeamAsync(bot);

        Assert.NotNull(team);
        var unitIds = JsonSerializer.Deserialize<List<Guid>>(team.UnitIdsJson);
        Assert.NotNull(unitIds);
        Assert.True(unitIds!.Count > 0);

        // All unit IDs should be template units
        var templateIds = await _context.Units.Where(u => u.IsTemplate).Select(u => u.Id).ToListAsync();
        Assert.All(unitIds, id => Assert.Contains(id, templateIds));
    }

    [Fact]
    public async Task CreateBotTeam_Returns5Units()
    {
        var bot = CreateBot(rating: 1200);

        var team = await _service.CreateBotTeamAsync(bot);

        var unitIds = JsonSerializer.Deserialize<List<Guid>>(team.UnitIdsJson);
        Assert.Equal(5, unitIds!.Count);
    }

    [Fact]
    public async Task CreateBotTeam_NonBotPlayer_Throws()
    {
        var humanPlayer = new Player
        {
            Id = Guid.NewGuid(),
            Username = "human",
            Email = "human@test.com",
            PasswordHash = "hash",
            IsBot = false,
            Rating = 1000,
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow
        };
        _context.Players.Add(humanPlayer);
        _context.SaveChanges();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateBotTeamAsync(humanPlayer));
    }

    [Fact]
    public async Task CreateBotTeam_LowRating_SimpleComposition()
    {
        var bot = CreateBot("BeginnerBot", rating: 800);

        var team = await _service.CreateBotTeamAsync(bot);

        // Low-rating bots should have a strategy with "balanced" formation
        var strategy = JsonSerializer.Deserialize<JsonElement>(team.StrategyJson);
        var formation = strategy.GetProperty("Formation").GetString()
            ?? strategy.GetProperty("formation").GetString();
        Assert.Equal("balanced", formation);
    }

    [Fact]
    public async Task CreateBotTeam_HighRating_DiverseComposition()
    {
        var bot = CreateBot("ExpertBot", rating: 1500);

        var team = await _service.CreateBotTeamAsync(bot);

        // High-rating bots should have abilities in their strategy
        var strategy = JsonSerializer.Deserialize<JsonElement>(team.StrategyJson);

        // Expert bots have conditional abilities
        bool hasAbilities = false;
        if (strategy.TryGetProperty("Abilities", out var abilities) || strategy.TryGetProperty("abilities", out abilities))
        {
            hasAbilities = abilities.ValueKind == JsonValueKind.Object &&
                          abilities.EnumerateObject().Any();
        }
        Assert.True(hasAbilities, "Expert bot should have ability conditions in strategy");
    }

    [Fact]
    public async Task EnsureBotHasTeam_ExistingTeam_Noop()
    {
        var bot = CreateBot("ExistingTeamBot", rating: 1000);

        // Create a team first
        await _service.CreateBotTeamAsync(bot);
        var teamsBefore = await _context.Teams.CountAsync(t => t.PlayerId == bot.Id);

        // Ensure doesn't duplicate
        await _service.EnsureBotHasTeamAsync(bot);

        var teamsAfter = await _context.Teams.CountAsync(t => t.PlayerId == bot.Id);
        Assert.Equal(teamsBefore, teamsAfter);
    }

    [Fact]
    public async Task EnsureBotHasTeam_NoTeam_CreatesOne()
    {
        var bot = CreateBot("NoTeamBot", rating: 1100);

        var teamsBefore = await _context.Teams.CountAsync(t => t.PlayerId == bot.Id);
        Assert.Equal(0, teamsBefore);

        await _service.EnsureBotHasTeamAsync(bot);

        var teamsAfter = await _context.Teams.CountAsync(t => t.PlayerId == bot.Id);
        Assert.Equal(1, teamsAfter);
    }

    [Fact]
    public async Task CreateBotTeam_NoTemplates_Throws()
    {
        // Clear all templates
        var templates = await _context.Units.Where(u => u.IsTemplate).ToListAsync();
        _context.Units.RemoveRange(templates);
        await _context.SaveChangesAsync();

        var bot = CreateBot("NoTemplatesBot", rating: 1000);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateBotTeamAsync(bot));
    }
}
