using System.Security.Claims;
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
public class StrategyController : ControllerBase
{
    private readonly IStrategyMarketplaceService _marketplace;

    public StrategyController(IStrategyMarketplaceService marketplace)
    {
        _marketplace = marketplace;
    }

    /// <summary>
    /// Browse public strategies.
    /// </summary>
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

    /// <summary>
    /// Upload a new strategy to the marketplace.
    /// </summary>
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

    /// <summary>
    /// Download (purchase) a strategy.
    /// </summary>
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

    /// <summary>
    /// Rate a strategy (1-5 stars).
    /// </summary>
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
