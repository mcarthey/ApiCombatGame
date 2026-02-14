using System.Security.Claims;
using ApiCombatGame.Filters.Attributes;
using ApiCombatGame.Models.DTOs.Loot;
using ApiCombatGame.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiCombatGame.Controllers;

/// <summary>
/// Random loot drops earned from battles.
/// </summary>
[ApiCategoryMeta("gift", "#f97316", Order = 15)]
[ApiController]
[Route("api/v1/loot")]
[Authorize]
[Tags("Loot")]
public class LootController : ControllerBase
{
    private readonly ILootService _lootService;

    public LootController(ILootService lootService)
    {
        _lootService = lootService;
    }

    /// <summary>Get your unclaimed loot drops.</summary>
    /// <remarks>
    /// After each battle, you have a chance to receive random loot drops. Base drop chance is 15%,
    /// increasing with win streaks (up to 25% at 3+ wins). Premium players get a guaranteed drop
    /// every 5 battles.
    ///
    /// **Drop Types**:
    /// - **CurrencyPack**: Random gold (50-800g)
    /// - **XpBoost**: Bonus XP (50-200)
    /// - **CriticalGold**: JACKPOT! 3x your base battle reward (5% chance, independent roll)
    /// - **RareTitle**: An exclusive display title (5% of drops)
    ///
    /// Drops accumulate until claimed. Use POST /api/v1/loot/claim to collect everything at once.
    /// </remarks>
    /// <response code="200">List of unclaimed loot drops.</response>
    [ApiDifficulty("beginner")]
    [ApiGameTip("Loot drops stack up! Check your pending loot after a battle session and claim them all at once for a satisfying gold dump.")]
    [ApiGameTip("Win streaks increase your drop chance from 15% to 25%. Going on a 3+ win streak means roughly 1 in 4 battles will drop bonus loot.")]
    [ApiPrerequisite("Complete at least one battle")]
    [HttpGet("pending")]
    [ProducesResponseType(typeof(PendingLootResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPendingLoot()
    {
        var playerId = GetPlayerId();
        var result = await _lootService.GetPendingLootAsync(playerId);
        return Ok(result);
    }

    /// <summary>Claim all pending loot drops.</summary>
    /// <remarks>
    /// Claims all unclaimed loot drops, awarding gold, XP, and any titles earned.
    /// Returns a summary of everything claimed.
    /// </remarks>
    /// <response code="200">Loot successfully claimed with totals.</response>
    /// <response code="400">No unclaimed loot drops to claim.</response>
    [ApiDifficulty("beginner")]
    [ApiGameTip("Don't sit on unclaimed loot for too long. That gold could be funding your next unit unlock or team upgrade right now!")]
    [ApiPrerequisite("Have unclaimed loot drops")]
    [ApiExample("Claim All Loot", Response = "{\n  \"goldAwarded\": 750,\n  \"xpAwarded\": 150,\n  \"titlesUnlocked\": [\"Lucky Strike\"],\n  \"dropsClaimed\": 4\n}")]
    [HttpPost("claim")]
    [ProducesResponseType(typeof(ClaimLootResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ClaimLoot()
    {
        try
        {
            var playerId = GetPlayerId();
            var result = await _lootService.ClaimAllLootAsync(playerId);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    private Guid GetPlayerId()
    {
        var claim = User.FindFirst("PlayerId") ?? User.FindFirst(ClaimTypes.NameIdentifier);
        if (claim == null || !Guid.TryParse(claim.Value, out var playerId))
            throw new UnauthorizedAccessException("Invalid token.");
        return playerId;
    }
}
