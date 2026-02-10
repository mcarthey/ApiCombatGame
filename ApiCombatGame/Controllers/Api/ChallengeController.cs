using System.Security.Claims;
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
public class ChallengeController : ControllerBase
{
    private readonly IChallengeService _challengeService;

    public ChallengeController(IChallengeService challengeService)
    {
        _challengeService = challengeService;
    }

    /// <summary>
    /// Get player's active daily challenges.
    /// </summary>
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

    /// <summary>
    /// Claim reward for a completed challenge.
    /// </summary>
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
