using System.Security.Claims;
using ApiCombatGame.Filters.Attributes;
using ApiCombatGame.Models.DTOs.Referral;
using ApiCombatGame.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiCombatGame.Controllers;

/// <summary>
/// Referral program for viral growth.
/// </summary>
[ApiCategoryMeta("share", "#10b981", Order = 16)]
[ApiController]
[Route("api/v1/referral")]
[Authorize]
[Tags("Referral")]
public class ReferralController : ControllerBase
{
    private readonly IReferralService _referralService;

    public ReferralController(IReferralService referralService)
    {
        _referralService = referralService;
    }

    /// <summary>Get your referral code and stats.</summary>
    /// <remarks>
    /// Returns your unique referral code and how many players have signed up using it.
    /// Share your code with friends — when they redeem it, you both get rewarded:
    ///
    /// - **You (referrer)**: +500g per successful referral
    /// - **Them (new player)**: +300g bonus starting currency
    ///
    /// A new code is automatically generated each time one is redeemed,
    /// so you can always share your latest code.
    /// </remarks>
    /// <response code="200">Your referral code and statistics.</response>
    [ApiDifficulty("beginner")]
    [ApiGameTip("Share your referral code in Discord servers, Reddit posts, or with classmates. Each referral nets you 500g — that's enough to unlock a mid-tier unit.")]
    [ApiPrerequisite("Register and login")]
    [ApiExample("Get Referral Info", Response = "{\n  \"referralCode\": \"ABCD1234\",\n  \"totalReferrals\": 3,\n  \"totalGoldEarned\": 1500,\n  \"rewardPerReferral\": 500,\n  \"referredPlayerBonus\": 300\n}")]
    [HttpGet("info")]
    [ProducesResponseType(typeof(ReferralInfoResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReferralInfo()
    {
        var playerId = GetPlayerId();
        var result = await _referralService.GetReferralInfoAsync(playerId);
        return Ok(result);
    }

    /// <summary>Redeem a referral code.</summary>
    /// <remarks>
    /// Redeem a friend's referral code to receive a 300g bonus. You can only redeem one
    /// referral code per account, and you cannot use your own code.
    /// </remarks>
    /// <param name="code">The 8-character referral code to redeem.</param>
    /// <response code="200">Code redeemed successfully with bonus awarded.</response>
    /// <response code="400">Invalid code, self-referral, or code already used.</response>
    /// <response code="404">Code not found.</response>
    [ApiDifficulty("beginner")]
    [ApiGameTip("Redeem a referral code right after registering to start with 1,300g instead of 1,000g. That extra 300g lets you unlock your first unit immediately.")]
    [ApiPrerequisite("Register and login")]
    [ApiExample("Redeem Code", Response = "{\n  \"success\": true,\n  \"bonusGoldAwarded\": 300,\n  \"referrerUsername\": \"FriendlyPlayer\"\n}")]
    [HttpPost("redeem/{code}")]
    [ProducesResponseType(typeof(RedeemReferralResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RedeemCode(string code)
    {
        try
        {
            var playerId = GetPlayerId();
            var result = await _referralService.RedeemCodeAsync(playerId, code);
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

    /// <summary>View the referral leaderboard.</summary>
    /// <remarks>
    /// See who has referred the most players. Top referrers are driving community growth.
    /// </remarks>
    /// <param name="limit">Max entries (default 20, max 50).</param>
    /// <response code="200">Referral leaderboard with your rank.</response>
    [ApiDifficulty("beginner")]
    [ApiPrerequisite("Register and login")]
    [HttpGet("leaderboard")]
    [ProducesResponseType(typeof(ReferralLeaderboardResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLeaderboard([FromQuery] int limit = 20)
    {
        var playerId = GetPlayerId();
        limit = Math.Clamp(limit, 1, 50);
        var result = await _referralService.GetLeaderboardAsync(playerId, limit);
        return Ok(result);
    }

    private Guid GetPlayerId()
    {
        var claim = User.FindFirst("PlayerId") ?? User.FindFirst(ClaimTypes.NameIdentifier);
        if (claim == null || !Guid.TryParse(claim.Value, out var playerId))
            throw new UnauthorizedAccessException("Invalid token.");
        return playerId;
    }
}
