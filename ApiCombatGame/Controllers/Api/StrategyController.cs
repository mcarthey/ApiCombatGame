using System.Security.Claims;
using ApiCombatGame.Filters.Attributes;
using ApiCombatGame.Models.DTOs.Marketplace;
using ApiCombatGame.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiCombatGame.Controllers.Api;

/// <summary>
/// Strategy marketplace for sharing and trading battle strategies.
/// </summary>
[ApiController]
[Route("api/v1/strategies")]
[Authorize]
[Tags("Strategy Marketplace")]
[ApiCategoryMeta("store", "#10b981", Order = 6)]
public class StrategyController : ControllerBase
{
    private readonly IStrategyMarketplaceService _marketplace;

    public StrategyController(IStrategyMarketplaceService marketplace)
    {
        _marketplace = marketplace;
    }

    /// <summary>Browse the strategy marketplace.</summary>
    /// <remarks>
    /// Explore community-created battle strategies. No authentication required.
    ///
    /// Sort options: "popular" (most downloaded), "rating" (highest rated), "newest" (most recent).
    /// Each strategy shows download count, average rating, win rate, and effectiveness multiplier.
    /// </remarks>
    /// <param name="sortBy">Sort order: popular, rating, or newest. Default: popular.</param>
    /// <param name="limit">Results per page (default 20).</param>
    /// <param name="offset">Pagination offset (default 0).</param>
    /// <response code="200">Array of marketplace strategy listings.</response>
    [ApiDifficulty("beginner")]
    [ApiGameTip("Sort by rating to find proven strategies that consistently win battles.")]
    [ApiExample("Browse top-rated strategies", Request = "GET /api/v1/strategies/browse?sortBy=rating&limit=5&offset=0", Response = "[\n  {\n    \"strategyId\": \"a1b2c3d4-e5f6-7890-abcd-ef1234567890\",\n    \"name\": \"Blitz Rush Alpha\",\n    \"creatorName\": \"TacticalMind\",\n    \"price\": 150,\n    \"downloadCount\": 342,\n    \"averageRating\": 4.7,\n    \"winRate\": 68.5,\n    \"effectivenessMultiplier\": 1.15\n  }\n]")]
    [HttpGet("browse")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<StrategyResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<StrategyResponse>>> Browse(
        [FromQuery] string sortBy = "popular",
        [FromQuery] int limit = 20,
        [FromQuery] int offset = 0)
    {
        var strategies = await _marketplace.BrowseStrategies(sortBy, limit, offset);

        return Ok(strategies.Select(s => new StrategyResponse
        {
            StrategyId = s.Id,
            Name = s.Name,
            Description = s.Description,
            CreatorName = s.Creator?.Username ?? "Unknown",
            Price = s.Price,
            DownloadCount = s.DownloadCount,
            AverageRating = s.AverageRating,
            WinRate = s.WinCount + s.LossCount > 0
                ? (double)s.WinCount / (s.WinCount + s.LossCount) * 100
                : 0,
            EffectivenessMultiplier = s.EffectivenessMultiplier,
            CreatedAt = s.CreatedAt
        }));
    }

    /// <summary>Publish a strategy to the marketplace.</summary>
    /// <remarks>
    /// Share your battle strategy with the community. Set a price in currency (0 for free).
    /// You earn currency each time another player purchases your strategy.
    /// </remarks>
    /// <param name="request">Strategy name, description, JSON config, and price.</param>
    /// <response code="201">Strategy published to the marketplace.</response>
    /// <response code="400">Invalid strategy configuration or missing required fields.</response>
    [ApiDifficulty("advanced")]
    [ApiGameTip("You earn currency every time another player purchases your strategy.")]
    [ApiGameTip("Strategies with high win rates attract more downloads and generate more income.")]
    [ApiPrerequisite("Register and login", "Build and test your strategy in battles first")]
    [ApiExample("Publish a free strategy", Request = "{\n  \"name\": \"Defensive Turtle\",\n  \"description\": \"Tanky frontline with healer support\",\n  \"strategyJson\": \"{\\\"formation\\\":\\\"defensive\\\",\\\"priority\\\":\\\"survive\\\"}\",\n  \"price\": 0\n}", Response = "{\n  \"strategyId\": \"f47ac10b-58cc-4372-a567-0e02b2c3d479\",\n  \"name\": \"Defensive Turtle\",\n  \"description\": \"Tanky frontline with healer support\",\n  \"price\": 0,\n  \"createdAt\": \"2026-02-11T14:30:00Z\"\n}")]
    [HttpPost("upload")]
    [ProducesResponseType(typeof(StrategyResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<StrategyResponse>> Upload([FromBody] StrategyUploadRequest request)
    {
        var playerId = GetPlayerId();

        var strategy = await _marketplace.UploadStrategy(
            playerId,
            request.Name,
            request.Description,
            request.StrategyJson,
            request.Price
        );

        return CreatedAtAction(nameof(Browse), new StrategyResponse
        {
            StrategyId = strategy.Id,
            Name = strategy.Name,
            Description = strategy.Description,
            Price = strategy.Price,
            CreatedAt = strategy.CreatedAt
        });
    }

    /// <summary>Purchase and download a strategy.</summary>
    /// <remarks>
    /// Spend currency to acquire a marketplace strategy. The full strategy JSON is returned
    /// and can be used directly in your team configuration.
    /// </remarks>
    /// <param name="strategyId">The strategy to purchase.</param>
    /// <response code="200">Strategy details including the full JSON configuration.</response>
    /// <response code="400">Insufficient currency.</response>
    /// <response code="404">Strategy not found.</response>
    [ApiDifficulty("intermediate")]
    [ApiGameTip("Check the win rate and effectiveness multiplier before buying to ensure a good investment.")]
    [ApiPrerequisite("Register and login")]
    [HttpPost("{strategyId}/download")]
    [ProducesResponseType(typeof(StrategyDownloadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StrategyDownloadResponse>> Download(Guid strategyId)
    {
        try
        {
            var playerId = GetPlayerId();
            var strategy = await _marketplace.DownloadStrategy(strategyId, playerId);

            return Ok(new StrategyDownloadResponse
            {
                StrategyId = strategy.Id,
                Name = strategy.Name,
                StrategyJson = strategy.StrategyJson
            });
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

    /// <summary>Rate a marketplace strategy.</summary>
    /// <remarks>
    /// Submit a 1-5 star rating with optional comment. You can only rate strategies you have downloaded.
    /// </remarks>
    /// <param name="strategyId">The strategy to rate.</param>
    /// <param name="request">Star rating (1-5) and optional review comment.</param>
    /// <response code="200">Rating submitted.</response>
    /// <response code="404">Strategy not found.</response>
    [ApiDifficulty("beginner")]
    [ApiGameTip("Honest ratings help the community find the best strategies and reward quality creators.")]
    [ApiPrerequisite("Download a strategy")]
    [HttpPost("{strategyId}/rate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Rate(Guid strategyId, [FromBody] StrategyRatingRequest request)
    {
        try
        {
            var playerId = GetPlayerId();
            await _marketplace.RateStrategy(strategyId, playerId, request.Rating, request.Comment);
            return Ok(new { message = "Rating submitted" });
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
