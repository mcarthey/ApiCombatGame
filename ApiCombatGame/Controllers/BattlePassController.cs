using System.Security.Claims;
using ApiCombatGame.Filters.Attributes;
using ApiCombatGame.Models.DTOs.BattlePass;
using ApiCombatGame.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiCombatGame.Controllers;

/// <summary>
/// Seasonal battle pass with 30 levels of free and premium rewards.
/// </summary>
[ApiCategoryMeta("star", "#f59e0b", Order = 19)]
[ApiController]
[Route("api/v1/battlepass")]
[Authorize]
[Tags("Battle Pass")]
public class BattlePassController : ControllerBase
{
    private readonly IBattlePassService _battlePassService;

    public BattlePassController(IBattlePassService battlePassService)
    {
        _battlePassService = battlePassService;
    }

    /// <summary>Get your current battle pass progress.</summary>
    /// <remarks>
    /// Returns your current level, XP progress, and all rewards (claimed and unclaimed).
    /// Each season has a 30-level battle pass. Free-track rewards are available to everyone.
    /// Premium-track rewards unlock with Premium Plus subscription.
    ///
    /// XP sources: battles (+100 win / +25 loss), daily challenges (+50-200), daily login (+25).
    /// Premium Plus players earn 25% bonus battle pass XP.
    /// </remarks>
    /// <response code="200">Your battle pass progress with all reward details.</response>
    [ApiDifficulty("beginner")]
    [ApiGameTip("Battle pass XP comes from everything you do — battles, challenges, and daily logins. Play regularly to maximize your progress.")]
    [ApiGameTip("Premium Plus subscribers get the premium track automatically included, plus 25% bonus XP toward pass levels.")]
    [ApiPrerequisite("Register and login")]
    [ApiExample("Get Progress", Response = "{\n  \"passName\": \"Season 1 Battle Pass\",\n  \"currentLevel\": 12,\n  \"currentXp\": 450,\n  \"xpToNextLevel\": 550,\n  \"maxLevel\": 30,\n  \"overallProgressPercent\": 41.5,\n  \"hasPremium\": false,\n  \"rewards\": [...],\n  \"daysRemaining\": 34\n}")]
    [HttpGet("progress")]
    [ProducesResponseType(typeof(BattlePassProgressResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProgress()
    {
        var playerId = GetPlayerId();
        var result = await _battlePassService.GetProgressAsync(playerId);
        return Ok(result);
    }

    /// <summary>Claim a battle pass level reward.</summary>
    /// <remarks>
    /// Claims the reward at the specified level. You must have reached that level to claim.
    /// Free-track rewards are available to all players. Premium-track rewards require
    /// Premium Plus subscription.
    ///
    /// Each level has both a free and premium reward. Claiming checks free first, then premium.
    /// Rewards are granted immediately — currency goes to your balance, XP to your profile,
    /// titles to your collection.
    /// </remarks>
    /// <param name="level">The level number to claim (1-30).</param>
    /// <response code="200">Reward successfully claimed.</response>
    /// <response code="400">Level not reached or reward already claimed.</response>
    [ApiDifficulty("beginner")]
    [ApiGameTip("Don't forget to claim your rewards! They don't auto-grant — you need to claim each level manually.")]
    [ApiPrerequisite("Reach the specified battle pass level")]
    [ApiExample("Claim Level 5", Response = "{\n  \"success\": true,\n  \"level\": 5,\n  \"track\": \"free\",\n  \"rewardType\": \"currency\",\n  \"rewardValue\": 275,\n  \"description\": \"Level 5 Milestone: 250g\",\n  \"message\": \"Claimed Level 5 Milestone: 250g!\"\n}")]
    [HttpPost("claim/{level:int}")]
    [ProducesResponseType(typeof(ClaimRewardResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ClaimReward(int level)
    {
        if (level < 1 || level > 30)
            return BadRequest(new { error = "Level must be between 1 and 30." });

        var playerId = GetPlayerId();
        try
        {
            var result = await _battlePassService.ClaimRewardAsync(playerId, level);
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
