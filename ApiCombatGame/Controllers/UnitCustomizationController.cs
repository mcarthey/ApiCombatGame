using System.Security.Claims;
using ApiCombatGame.Data;
using ApiCombatGame.Filters.Attributes;
using ApiCombatGame.Models.DTOs.UnitCustomization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiCombatGame.Controllers;

/// <summary>
/// Gold sinks: rename, reroll stats, and apply golden upgrades to your units.
/// </summary>
[ApiCategoryMeta("sparkles", "#eab308", Order = 17)]
[ApiController]
[Route("api/v1/units")]
[Authorize]
[Tags("Unit Customization")]
public class UnitCustomizationController : ControllerBase
{
    private readonly GameDbContext _context;
    private const int RenameCost = 200;
    private const int RerollCost = 500;
    private const int GoldenUpgradeCost = 2000;

    public UnitCustomizationController(GameDbContext context)
    {
        _context = context;
    }

    /// <summary>Rename a unit you own.</summary>
    /// <remarks>
    /// Give your unit a custom display name. Costs 200g. The original name is preserved
    /// internally — your custom name appears in battle logs and your roster.
    ///
    /// Names must be 3-50 characters. No profanity filter (yet — be nice).
    /// </remarks>
    /// <param name="request">Unit ID and new name.</param>
    /// <response code="200">Unit renamed successfully.</response>
    /// <response code="400">Insufficient gold, invalid name, or unit not found.</response>
    [ApiDifficulty("beginner")]
    [ApiGameTip("Naming your units creates emotional attachment. You'll fight harder for 'Thunderstrike' than 'Bronze Knight'. That's psychology working in your favor.")]
    [ApiPrerequisite("Own at least one unit")]
    [ApiExample("Rename Unit", Request = "{\n  \"unitId\": \"...\",\n  \"newName\": \"Thunderstrike\"\n}", Response = "{\n  \"unitId\": \"...\",\n  \"name\": \"Bronze Knight\",\n  \"customName\": \"Thunderstrike\",\n  \"goldSpent\": 200,\n  \"remainingCurrency\": 800\n}")]
    [HttpPost("rename")]
    [ProducesResponseType(typeof(UnitCustomizationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RenameUnit([FromBody] RenameUnitRequest request)
    {
        var playerId = GetPlayerId();
        var player = await _context.Players.FindAsync(playerId);
        if (player == null) return NotFound(new { error = "Player not found." });

        var unit = await _context.Units.FirstOrDefaultAsync(u => u.Id == request.UnitId && u.PlayerId == playerId);
        if (unit == null) return BadRequest(new { error = "Unit not found or doesn't belong to you." });

        if (player.Currency < RenameCost)
            return BadRequest(new { error = $"Insufficient gold. Rename costs {RenameCost}g, you have {player.Currency}g." });

        player.Currency -= RenameCost;
        unit.CustomName = request.NewName;
        await _context.SaveChangesAsync();

        return Ok(ToResponse(unit, RenameCost, player.Currency));
    }

    /// <summary>Reroll a unit's stats.</summary>
    /// <remarks>
    /// Randomizes one stat within the unit's class range. Costs 500g per reroll.
    /// The stat chosen and new value are random — you might get a better roll or a worse one.
    /// Each subsequent reroll on the same unit costs 500g.
    ///
    /// **Stat ranges vary by class** — Warriors have higher base health, Mages have higher attack, etc.
    /// The reroll respects these class boundaries.
    /// </remarks>
    /// <param name="request">Unit ID to reroll.</param>
    /// <response code="200">Stat rerolled with new values.</response>
    /// <response code="400">Insufficient gold or unit not found.</response>
    [ApiDifficulty("intermediate")]
    [ApiGameTip("Rerolling is a gamble. Track your unit's stats before and after — if you get a worse roll, you can reroll again (for another 500g). High risk, high reward.")]
    [ApiPrerequisite("Own at least one unit", "Have 500g")]
    [HttpPost("reroll")]
    [ProducesResponseType(typeof(UnitCustomizationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RerollStats([FromBody] RerollStatsRequest request)
    {
        var playerId = GetPlayerId();
        var player = await _context.Players.FindAsync(playerId);
        if (player == null) return NotFound(new { error = "Player not found." });

        var unit = await _context.Units.FirstOrDefaultAsync(u => u.Id == request.UnitId && u.PlayerId == playerId);
        if (unit == null) return BadRequest(new { error = "Unit not found or doesn't belong to you." });

        if (player.Currency < RerollCost)
            return BadRequest(new { error = $"Insufficient gold. Reroll costs {RerollCost}g, you have {player.Currency}g." });

        var rng = new Random();
        var (hpRange, atkRange, defRange, spdRange) = GetStatRanges(unit.Class);

        // Pick a random stat to reroll
        int stat = rng.Next(4);
        switch (stat)
        {
            case 0: unit.Health = rng.Next(hpRange.Min, hpRange.Max + 1); break;
            case 1: unit.Attack = rng.Next(atkRange.Min, atkRange.Max + 1); break;
            case 2: unit.Defense = rng.Next(defRange.Min, defRange.Max + 1); break;
            case 3: unit.Speed = rng.Next(spdRange.Min, spdRange.Max + 1); break;
        }

        player.Currency -= RerollCost;
        unit.RerollCount++;
        await _context.SaveChangesAsync();

        return Ok(ToResponse(unit, RerollCost, player.Currency));
    }

    /// <summary>Apply golden upgrade to a unit.</summary>
    /// <remarks>
    /// Permanently boosts ALL stats by 5%. Costs 2000g and can only be applied once per unit.
    /// Golden units get a special designation visible in battle logs and your roster.
    ///
    /// This is the ultimate investment — make sure you're upgrading a unit you'll keep in your
    /// main team long-term.
    /// </remarks>
    /// <param name="request">Unit ID to upgrade.</param>
    /// <response code="200">Unit upgraded to golden status.</response>
    /// <response code="400">Already golden, insufficient gold, or unit not found.</response>
    [ApiDifficulty("intermediate")]
    [ApiGameTip("Golden upgrade your most-used units first. A 5% boost on a unit that fights every battle compounds into hundreds of extra damage over a season.")]
    [ApiPrerequisite("Own at least one unit", "Have 2000g")]
    [ApiExample("Golden Upgrade", Request = "{\n  \"unitId\": \"...\"\n}", Response = "{\n  \"unitId\": \"...\",\n  \"name\": \"Archmage\",\n  \"isGolden\": true,\n  \"health\": 105,\n  \"attack\": 47,\n  \"defense\": 15,\n  \"speed\": 21,\n  \"goldSpent\": 2000,\n  \"remainingCurrency\": 3000\n}")]
    [HttpPost("golden-upgrade")]
    [ProducesResponseType(typeof(UnitCustomizationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GoldenUpgrade([FromBody] GoldenUpgradeRequest request)
    {
        var playerId = GetPlayerId();
        var player = await _context.Players.FindAsync(playerId);
        if (player == null) return NotFound(new { error = "Player not found." });

        var unit = await _context.Units.FirstOrDefaultAsync(u => u.Id == request.UnitId && u.PlayerId == playerId);
        if (unit == null) return BadRequest(new { error = "Unit not found or doesn't belong to you." });

        if (unit.IsGolden)
            return BadRequest(new { error = "Unit is already golden. Each unit can only be upgraded once." });

        if (player.Currency < GoldenUpgradeCost)
            return BadRequest(new { error = $"Insufficient gold. Golden upgrade costs {GoldenUpgradeCost}g, you have {player.Currency}g." });

        player.Currency -= GoldenUpgradeCost;
        unit.IsGolden = true;

        // Apply +5% to all stats
        unit.Health = (int)(unit.Health * 1.05);
        unit.Attack = (int)(unit.Attack * 1.05);
        unit.Defense = (int)(unit.Defense * 1.05);
        unit.Speed = (int)(unit.Speed * 1.05);

        await _context.SaveChangesAsync();

        return Ok(ToResponse(unit, GoldenUpgradeCost, player.Currency));
    }

    private static UnitCustomizationResponse ToResponse(Models.Domain.Unit unit, int goldSpent, int remaining) => new()
    {
        UnitId = unit.Id,
        Name = unit.Name,
        CustomName = unit.CustomName,
        IsGolden = unit.IsGolden,
        RerollCount = unit.RerollCount,
        Health = unit.Health,
        Attack = unit.Attack,
        Defense = unit.Defense,
        Speed = unit.Speed,
        GoldSpent = goldSpent,
        RemainingCurrency = remaining
    };

    private static (StatRange Hp, StatRange Atk, StatRange Def, StatRange Spd) GetStatRanges(Models.Enums.UnitClass unitClass)
    {
        return unitClass switch
        {
            Models.Enums.UnitClass.Warrior => (new(100, 160), new(18, 35), new(14, 25), new(8, 15)),
            Models.Enums.UnitClass.Mage => (new(65, 100), new(25, 45), new(8, 15), new(10, 20)),
            Models.Enums.UnitClass.Ranger => (new(75, 110), new(22, 40), new(10, 18), new(14, 28)),
            Models.Enums.UnitClass.Healer => (new(70, 105), new(12, 25), new(14, 26), new(10, 16)),
            Models.Enums.UnitClass.Tank => (new(130, 200), new(16, 28), new(25, 40), new(5, 8)),
            _ => (new(80, 150), new(15, 35), new(10, 30), new(8, 20))
        };
    }

    private record StatRange(int Min, int Max);

    private Guid GetPlayerId()
    {
        var claim = User.FindFirst("PlayerId") ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (claim == null || !Guid.TryParse(claim.Value, out var playerId))
            throw new UnauthorizedAccessException("Invalid token.");
        return playerId;
    }
}
