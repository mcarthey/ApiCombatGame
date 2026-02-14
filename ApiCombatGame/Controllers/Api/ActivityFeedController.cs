using System.Security.Claims;
using ApiCombatGame.Filters.Attributes;
using ApiCombatGame.Models.DTOs.Activity;
using ApiCombatGame.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiCombatGame.Controllers.Api;

/// <summary>
/// Activity feed and lifetime stats for social engagement.
/// </summary>
[ApiController]
[Route("api/v1/activity")]
[Authorize]
[Tags("Activity Feed")]
[ApiCategoryMeta("activity", "#06b6d4", Order = 24)]
public class ActivityFeedController : ControllerBase
{
    private readonly IActivityFeedService _activityService;

    public ActivityFeedController(IActivityFeedService activityService)
    {
        _activityService = activityService;
    }

    /// <summary>Get your activity feed.</summary>
    /// <remarks>
    /// Returns a chronological feed of your recent game activities — battles fought,
    /// achievements unlocked, level-ups, and more. Paginated for easy consumption.
    /// </remarks>
    /// <param name="page">Page number (default 1).</param>
    /// <param name="pageSize">Items per page (default 20, max 50).</param>
    /// <response code="200">Paginated list of your activities.</response>
    [ApiDifficulty("beginner")]
    [ApiGameTip("Use the activity feed to track your progress and see a narrative of your game journey.")]
    [ApiPrerequisite("Register and login")]
    [HttpGet("feed")]
    [ProducesResponseType(typeof(ActivityFeedResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ActivityFeedResponse>> GetMyFeed(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var playerId = GetPlayerId();
        pageSize = Math.Clamp(pageSize, 1, 50);
        var feed = await _activityService.GetFeedAsync(playerId, page, pageSize);
        return Ok(feed);
    }

    /// <summary>Get another player's public activity feed.</summary>
    /// <remarks>
    /// View the public activity feed for any player by username. Only public activities
    /// are shown — private events are hidden.
    /// </remarks>
    /// <param name="username">The player's username.</param>
    /// <param name="page">Page number (default 1).</param>
    /// <param name="pageSize">Items per page (default 20, max 50).</param>
    /// <response code="200">Public activity feed.</response>
    /// <response code="404">Player not found.</response>
    [ApiDifficulty("beginner")]
    [ApiGameTip("Scout your opponents' activity feeds to learn their strategies and playstyle.")]
    [ApiPrerequisite("Register and login")]
    [HttpGet("feed/{username}")]
    [ProducesResponseType(typeof(ActivityFeedResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ActivityFeedResponse>> GetPlayerFeed(
        string username, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            pageSize = Math.Clamp(pageSize, 1, 50);
            var feed = await _activityService.GetPublicFeedAsync(username, page, pageSize);
            return Ok(feed);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>Get your lifetime stats (sunk cost profile).</summary>
    /// <remarks>
    /// A comprehensive breakdown of everything you've invested in the game:
    /// total battles, gold earned, win rate, achievements, time played, and more.
    /// See the full picture of your gaming journey.
    /// </remarks>
    /// <response code="200">Complete lifetime stats breakdown.</response>
    [ApiDifficulty("beginner")]
    [ApiGameTip("Show off your lifetime stats to flex your dedication. Total gold earned is the ultimate brag.")]
    [ApiPrerequisite("Register and login")]
    [ApiExample("Get lifetime stats", Response = "{\n  \"username\": \"DragonSlayer\",\n  \"level\": 42,\n  \"rating\": 1850,\n  \"totalBattlesPlayed\": 347,\n  \"totalBattlesWon\": 198,\n  \"winRate\": 57.1,\n  \"highestRating\": 1923,\n  \"totalGoldEarned\": 125000,\n  \"achievementsUnlocked\": 28,\n  \"daysPlayed\": 65,\n  \"investmentSummary\": \"347 battles fought | 125,000g earned | reached level 42 | playing for 65 days\"\n}")]
    [HttpGet("stats")]
    [ProducesResponseType(typeof(PlayerLifetimeStatsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<PlayerLifetimeStatsResponse>> GetMyStats()
    {
        var playerId = GetPlayerId();
        var stats = await _activityService.GetLifetimeStatsAsync(playerId);
        return Ok(stats);
    }

    /// <summary>Get another player's public lifetime stats.</summary>
    /// <remarks>
    /// View the lifetime stats for any player by username. Compare your progress
    /// against rivals and friends.
    /// </remarks>
    /// <param name="username">The player's username.</param>
    /// <response code="200">Player's lifetime stats.</response>
    /// <response code="404">Player not found.</response>
    [ApiDifficulty("beginner")]
    [ApiPrerequisite("Register and login")]
    [HttpGet("stats/{username}")]
    [ProducesResponseType(typeof(PlayerLifetimeStatsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlayerLifetimeStatsResponse>> GetPlayerStats(string username)
    {
        try
        {
            var stats = await _activityService.GetPublicStatsAsync(username);
            return Ok(stats);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
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
