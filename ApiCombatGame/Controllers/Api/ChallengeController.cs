using System.Security.Claims;
using ApiCombatGame.Filters.Attributes;
using ApiCombatGame.Models.DTOs.Challenge;
using ApiCombatGame.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiCombatGame.Controllers.Api;

/// <summary>
/// Daily challenges with personalized objectives.
/// </summary>
[ApiController]
[Route("api/v1/challenges")]
[Authorize]
[Tags("Challenges")]
[ApiCategoryMeta("target", "#f97316", Order = 7)]
public class ChallengeController : ControllerBase
{
    private readonly IChallengeService _challengeService;

    public ChallengeController(IChallengeService challengeService)
    {
        _challengeService = challengeService;
    }

    /// <summary>Get your active daily challenges.</summary>
    /// <remarks>
    /// Returns your current set of daily challenges with progress tracking.
    /// New challenges generate every 24 hours. Complete them by playing battles
    /// that meet the criteria, then claim rewards before they expire.
    /// </remarks>
    /// <response code="200">Array of active challenges with progress and reward info.</response>
    [ApiDifficulty("beginner")]
    [ApiGameTip("Check your daily challenges before queueing battles to maximize reward earnings.")]
    [ApiPrerequisite("Register and login")]
    [ApiExample("Get daily challenges", Response = "[\n  {\n    \"challengeId\": \"c9a1b2d3-4e5f-6789-abcd-ef0123456789\",\n    \"name\": \"Win 3 Battles\",\n    \"description\": \"Win 3 ranked battles today\",\n    \"progress\": 1,\n    \"requiredProgress\": 3,\n    \"isCompleted\": false,\n    \"rewardCurrency\": 200,\n    \"rewardExperience\": 500,\n    \"expiresAt\": \"2026-02-12T00:00:00Z\"\n  }\n]")]
    [HttpGet("daily")]
    [ProducesResponseType(typeof(List<ChallengeResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ChallengeResponse>>> GetDailyChallenges()
    {
        var playerId = GetPlayerId();
        var challenges = await _challengeService.GetActiveChallenges(playerId);

        return Ok(challenges.Select(c => new ChallengeResponse
        {
            ChallengeId = c.Id,
            Name = c.Name,
            Description = c.Description,
            Progress = c.Progress,
            RequiredProgress = c.RequiredProgress,
            IsCompleted = c.IsCompleted,
            RewardCurrency = c.RewardCurrency,
            RewardExperience = c.RewardExperience,
            ExpiresAt = c.ExpiresAt
        }));
    }

    /// <summary>Claim a completed challenge reward.</summary>
    /// <remarks>
    /// Collect currency and experience for a completed challenge. The challenge must show
    /// isCompleted: true before you can claim. Each challenge can only be claimed once.
    /// </remarks>
    /// <param name="request">The challenge ID to claim.</param>
    /// <response code="200">Reward claimed successfully.</response>
    /// <response code="400">Challenge not yet completed or already claimed.</response>
    /// <response code="404">Challenge not found.</response>
    [ApiDifficulty("beginner")]
    [ApiGameTip("Claim your rewards before the 24-hour expiry or they will be lost forever.")]
    [ApiPrerequisite("Complete a challenge objective")]
    [HttpPost("claim")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> ClaimReward([FromBody] ClaimRequest request)
    {
        try
        {
            var playerId = GetPlayerId();
            await _challengeService.ClaimReward(request.ChallengeId, playerId);
            return Ok(new { message = "Reward claimed successfully" });
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
