using System.Security.Claims;
using ApiCombatGame.Filters.Attributes;
using ApiCombatGame.Models.DTOs.Challenge;
using ApiCombatGame.Models.DTOs.Common;
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

        return Ok(challenges.Select(c =>
        {
            var r = new ChallengeResponse
            {
                ChallengeId = c.Id,
                Name = c.Name,
                Description = c.Description,
                Difficulty = c.Difficulty,
                BattlePassXp = c.Difficulty switch { "easy" => 50, "hard" => 200, _ => 100 },
                Progress = c.Progress,
                RequiredProgress = c.RequiredProgress,
                IsCompleted = c.IsCompleted,
                RewardCurrency = c.RewardCurrency,
                RewardExperience = c.RewardExperience,
                ExpiresAt = c.ExpiresAt
            };
            r.Links = new Dictionary<string, ApiLink>
            {
                ["self"] = Links.Get("/api/v1/challenges/daily")
            };
            if (c.IsCompleted)
                r.Links["claim"] = Links.Post("/api/v1/challenges/claim", "Claim this challenge reward");
            return r;
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

    /// <summary>Refresh your daily challenges (Premium only).</summary>
    /// <remarks>
    /// Replaces all uncompleted challenges with a fresh set. Premium and Premium Plus
    /// subscribers can refresh once per day. Already-completed challenges are kept.
    ///
    /// Each refresh generates one easy, one medium, and one hard challenge — giving you
    /// a better shot at rewards that match your skill level.
    /// </remarks>
    /// <response code="200">Challenges refreshed. Fetch `/daily` to see new ones.</response>
    /// <response code="400">Not a Premium subscriber or no challenges to refresh.</response>
    [ApiDifficulty("beginner")]
    [ApiGameTip("Use refresh strategically — if you get a hard challenge you can't complete, swap it for a new set.")]
    [ApiPrerequisite("Premium or Premium Plus subscription")]
    [HttpPost("refresh")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> RefreshChallenges()
    {
        try
        {
            var playerId = GetPlayerId();
            await _challengeService.RefreshChallengesAsync(playerId);
            return Ok(new { message = "Daily challenges refreshed! Check /daily for your new objectives." });
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
