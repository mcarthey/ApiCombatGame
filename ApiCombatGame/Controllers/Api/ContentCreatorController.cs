using System.Security.Claims;
using ApiCombatGame.Filters.Attributes;
using ApiCombatGame.Models.DTOs.Creator;
using ApiCombatGame.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiCombatGame.Controllers.Api;

/// <summary>
/// Content creator program — apply, track content, and earn rewards.
/// </summary>
[ApiController]
[Route("api/v1/creators")]
[Authorize]
[Tags("Creators")]
[ApiCategoryMeta("star", "#f59e0b", Order = 28)]
public class ContentCreatorController : ControllerBase
{
    private readonly IContentCreatorService _creatorService;

    public ContentCreatorController(IContentCreatorService creatorService)
    {
        _creatorService = creatorService;
    }

    /// <summary>Apply to become a content creator.</summary>
    /// <remarks>
    /// Submit your application to join the content creator program. Once verified by admins,
    /// you'll earn gems from strategy downloads, get a "creator" badge, and be eligible
    /// for the monthly spotlight.
    /// </remarks>
    /// <param name="request">Creator name and bio.</param>
    /// <response code="201">Application submitted.</response>
    /// <response code="400">Already applied or invalid data.</response>
    [ApiDifficulty("beginner")]
    [ApiGameTip("Upload a few strategies before applying — active creators get verified faster.")]
    [ApiPrerequisite("Register and login")]
    [ApiExample("Apply as creator", Request = "{\n  \"creatorName\": \"StrategyKing\",\n  \"bio\": \"I create advanced team compositions and publish educational guides.\"\n}")]
    [HttpPost("apply")]
    [ProducesResponseType(typeof(CreatorProfileResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreatorProfileResponse>> Apply([FromBody] ApplyCreatorRequest request)
    {
        try
        {
            var playerId = GetPlayerId();
            var profile = await _creatorService.ApplyAsync(playerId, request);
            return Created($"/api/v1/creators/me", profile);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Get your creator profile.</summary>
    /// <response code="200">Creator profile (or null if not applied).</response>
    [ApiDifficulty("beginner")]
    [ApiPrerequisite("Apply as content creator")]
    [HttpGet("me")]
    [ProducesResponseType(typeof(CreatorProfileResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<CreatorProfileResponse?>> GetMyProfile()
    {
        var playerId = GetPlayerId();
        var profile = await _creatorService.GetProfileAsync(playerId);
        return Ok(profile);
    }

    /// <summary>Get your detailed creator stats.</summary>
    /// <remarks>
    /// View your content performance: strategy download counts, ratings,
    /// educational module enrollments, and gem earnings.
    /// </remarks>
    /// <response code="200">Detailed creator statistics.</response>
    /// <response code="404">No creator profile found.</response>
    [ApiDifficulty("beginner")]
    [ApiPrerequisite("Apply as content creator")]
    [HttpGet("stats")]
    [ProducesResponseType(typeof(CreatorStatsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CreatorStatsResponse>> GetMyStats()
    {
        try
        {
            var playerId = GetPlayerId();
            var stats = await _creatorService.GetCreatorStatsAsync(playerId);
            return Ok(stats);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>Browse verified content creators.</summary>
    /// <remarks>
    /// Lists all verified creators, sorted by total downloads. Verified creators
    /// have been reviewed and approved by admins.
    /// </remarks>
    /// <response code="200">List of verified creators.</response>
    [ApiDifficulty("beginner")]
    [HttpGet("verified")]
    [ProducesResponseType(typeof(List<CreatorProfileResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<CreatorProfileResponse>>> GetVerifiedCreators()
    {
        var creators = await _creatorService.GetVerifiedCreatorsAsync();
        return Ok(creators);
    }

    /// <summary>Get this month's creator spotlight.</summary>
    /// <remarks>
    /// View the featured creators for the current month. Spotlighted creators
    /// are selected based on content quality, download volume, and community impact.
    /// </remarks>
    /// <response code="200">Monthly spotlight with featured creators.</response>
    [ApiDifficulty("beginner")]
    [ApiGameTip("Check the spotlight each month to discover new strategies from top creators.")]
    [HttpGet("spotlight")]
    [ProducesResponseType(typeof(SpotlightResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<SpotlightResponse>> GetSpotlight()
    {
        var spotlight = await _creatorService.GetSpotlightAsync();
        return Ok(spotlight);
    }

    private Guid GetPlayerId()
    {
        var claim = User.FindFirst("PlayerId") ?? User.FindFirst(ClaimTypes.NameIdentifier);
        if (claim == null || !Guid.TryParse(claim.Value, out var playerId))
            throw new UnauthorizedAccessException("Invalid token.");
        return playerId;
    }
}
