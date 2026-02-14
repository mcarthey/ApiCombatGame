using System.Security.Claims;
using ApiCombatGame.Filters.Attributes;
using ApiCombatGame.Models.DTOs.Season;
using ApiCombatGame.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiCombatGame.Controllers;

/// <summary>
/// Ranked seasons with tier progression and end-of-season rewards.
/// </summary>
[ApiCategoryMeta("trophy", "#f59e0b", Order = 14)]
[ApiController]
[Route("api/v1/season")]
[Authorize]
[Tags("Ranked Seasons")]
public class SeasonController : ControllerBase
{
    private readonly ISeasonService _seasonService;

    public SeasonController(ISeasonService seasonService)
    {
        _seasonService = seasonService;
    }

    /// <summary>Get the current ranked season and your progress.</summary>
    /// <remarks>
    /// Returns the active season details along with your current tier, rating, win/loss record,
    /// and progress toward the next tier. Seasons last 8 weeks. At the end of each season,
    /// your rating soft-resets and you earn rewards based on your peak tier.
    ///
    /// **Tier Thresholds**: Bronze (0) → Silver (1000) → Gold (1200) → Platinum (1400) → Diamond (1600) → Legend (1800)
    ///
    /// If no season exists, one is automatically created when you first call this endpoint.
    /// </remarks>
    /// <response code="200">Current season info with your ranking progress.</response>
    [ApiDifficulty("beginner")]
    [ApiGameTip("Check your season progress daily. Knowing exactly how many rating points you need for the next tier helps you decide whether to play it safe or push for promotion.")]
    [ApiGameTip("End-of-season rewards are based on your PEAK tier, not your current tier. Once you hit Diamond, your reward is locked even if you drop back to Platinum.")]
    [ApiPrerequisite("Register and login")]
    [ApiExample("Get Current Season", Response = "{\n  \"seasonId\": \"...\",\n  \"name\": \"Season 1: Dawn of Battle\",\n  \"seasonNumber\": 1,\n  \"daysRemaining\": 42,\n  \"currentTier\": \"Silver\",\n  \"seasonRating\": 1050,\n  \"peakRating\": 1120,\n  \"peakTier\": \"Gold\",\n  \"wins\": 15,\n  \"losses\": 8,\n  \"winRate\": 65.2,\n  \"ratingToNextTier\": 150,\n  \"nextTier\": \"Gold\"\n}")]
    [HttpGet("current")]
    [ProducesResponseType(typeof(CurrentSeasonResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCurrentSeason()
    {
        try
        {
            var playerId = GetPlayerId();
            var result = await _seasonService.GetCurrentSeasonAsync(playerId);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Get the season leaderboard.</summary>
    /// <remarks>
    /// Returns ranked players ordered by season rating. Only players with at least one ranked
    /// battle this season appear on the leaderboard. Your own rank is included in the response.
    /// </remarks>
    /// <param name="limit">Max entries to return (default 50, max 100).</param>
    /// <param name="offset">Entries to skip for pagination (default 0).</param>
    /// <response code="200">Season leaderboard with rankings.</response>
    [ApiDifficulty("beginner")]
    [ApiGameTip("Study the top players' win rates and tiers. If the #1 player has a 70% win rate, that's the benchmark. Compare your win rate to gauge how close you are to climbing.")]
    [ApiPrerequisite("Register and login")]
    [HttpGet("leaderboard")]
    [ProducesResponseType(typeof(SeasonLeaderboardResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLeaderboard([FromQuery] int limit = 50, [FromQuery] int offset = 0)
    {
        var playerId = GetPlayerId();
        limit = Math.Clamp(limit, 1, 100);
        offset = Math.Max(0, offset);
        var result = await _seasonService.GetSeasonLeaderboardAsync(playerId, limit, offset);
        return Ok(result);
    }

    /// <summary>Claim end-of-season rewards.</summary>
    /// <remarks>
    /// After a season ends, claim your rewards based on your peak tier. Rewards include gold,
    /// XP, and exclusive titles for Gold tier and above. Rewards can only be claimed once per season.
    ///
    /// **Rewards by Peak Tier**:
    /// - Bronze: 100g + 50 XP
    /// - Silver: 250g + 100 XP
    /// - Gold: 500g + 200 XP + "Gold Gladiator" title
    /// - Platinum: 1,000g + 400 XP + "Platinum Warrior" title
    /// - Diamond: 2,000g + 800 XP + "Diamond Champion" title
    /// - Legend: 5,000g + 1,500 XP + "Legendary Conqueror" title
    /// </remarks>
    /// <param name="seasonId">The ended season to claim rewards for.</param>
    /// <response code="200">Rewards successfully claimed.</response>
    /// <response code="400">Season still active or rewards already claimed.</response>
    /// <response code="404">Season or player rank not found.</response>
    [ApiDifficulty("beginner")]
    [ApiGameTip("Don't forget to claim rewards from past seasons! Check for unclaimed rewards after each season ends. The gold and XP can give you a head start in the new season.")]
    [ApiPrerequisite("Complete a ranked season", "Season must have ended")]
    [ApiExample("Claim Season Rewards", Response = "{\n  \"seasonName\": \"Season 1: Dawn of Battle\",\n  \"peakTier\": \"Gold\",\n  \"goldReward\": 500,\n  \"xpReward\": 200,\n  \"exclusiveTitle\": \"Gold Gladiator\",\n  \"claimed\": true\n}")]
    [HttpPost("rewards/{seasonId}")]
    [ProducesResponseType(typeof(SeasonRewardsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ClaimRewards(Guid seasonId)
    {
        try
        {
            var playerId = GetPlayerId();
            var result = await _seasonService.ClaimSeasonRewardsAsync(playerId, seasonId);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
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
