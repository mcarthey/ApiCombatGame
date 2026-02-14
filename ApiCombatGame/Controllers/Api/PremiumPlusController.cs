using System.Security.Claims;
using ApiCombatGame.Filters.Attributes;
using ApiCombatGame.Models.DTOs.Premium;
using ApiCombatGame.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiCombatGame.Controllers.Api;

/// <summary>
/// Premium Plus exclusive perks and benefits.
/// </summary>
[ApiController]
[Route("api/v1/premium")]
[Authorize]
[Tags("Premium Plus")]
[ApiCategoryMeta("crown", "#f59e0b", Order = 23)]
public class PremiumPlusController : ControllerBase
{
    private readonly IPremiumPlusService _premiumService;

    public PremiumPlusController(IPremiumPlusService premiumService)
    {
        _premiumService = premiumService;
    }

    /// <summary>View all perks for your current subscription tier.</summary>
    /// <remarks>
    /// Returns a complete breakdown of every benefit your subscription tier provides,
    /// including multipliers, limits, and exclusive features. Free players can see what
    /// they're missing — use this to decide if upgrading is worth it.
    /// </remarks>
    /// <response code="200">Perk breakdown for your current tier.</response>
    [ApiDifficulty("beginner")]
    [ApiGameTip("Compare Free vs Premium vs Premium Plus perks to find the best value for your playstyle.")]
    [ApiPrerequisite("Register and login")]
    [ApiExample("Get perks for Premium Plus", Response = "{\n  \"currentTier\": \"PremiumPlus\",\n  \"goldMultiplier\": 2.0,\n  \"xpMultiplier\": 1.5,\n  \"battlePassXpBonus\": 0.25,\n  \"dailyBattleLimit\": -1,\n  \"maxTeamSlots\": 10,\n  \"canCreateGuild\": true,\n  \"canRefreshChallenges\": true,\n  \"guaranteedLootEveryNBattles\": 3,\n  \"hasPremiumBattlePassTrack\": true,\n  \"monthlyGemStipend\": 500,\n  \"hasCreatorBadge\": true,\n  \"hasExclusiveCosmetics\": true,\n  \"lootRarityBoost\": 0.15,\n  \"badge\": \"creator\"\n}")]
    [HttpGet("perks")]
    [ProducesResponseType(typeof(PremiumPerksResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<PremiumPerksResponse>> GetPerks()
    {
        var playerId = GetPlayerId();
        var perks = await _premiumService.GetPerksAsync(playerId);
        return Ok(perks);
    }

    /// <summary>Claim your monthly gem stipend (Premium Plus only).</summary>
    /// <remarks>
    /// Premium Plus subscribers receive 500 free gems every 30 days. Claim them here.
    /// The timer resets from your last claim date, not the calendar month.
    /// </remarks>
    /// <response code="200">Gems claimed successfully.</response>
    /// <response code="400">Not eligible or already claimed this period.</response>
    [ApiDifficulty("beginner")]
    [ApiGameTip("Don't forget to claim your monthly gems — they don't auto-deposit!")]
    [ApiPrerequisite("Premium Plus subscription")]
    [HttpPost("claim-gems")]
    [ProducesResponseType(typeof(GemStipendClaimResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GemStipendClaimResponse>> ClaimMonthlyGems()
    {
        try
        {
            var playerId = GetPlayerId();
            var result = await _premiumService.ClaimMonthlyGemsAsync(playerId);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Set your profile badge.</summary>
    /// <remarks>
    /// Equip a badge that displays on your profile. Different tiers unlock different badges.
    /// Premium Plus members get exclusive badges like "creator", "elite", and "vip".
    /// Send null to remove your badge.
    /// </remarks>
    /// <param name="request">Badge to equip.</param>
    /// <response code="200">Badge updated.</response>
    /// <response code="400">Badge not available for your tier.</response>
    [ApiDifficulty("beginner")]
    [ApiGameTip("The 'creator' badge is Premium Plus exclusive — show off your supporter status!")]
    [ApiPrerequisite("Register and login")]
    [ApiExample("Set creator badge", Request = "{ \"badge\": \"creator\" }")]
    [HttpPost("badge")]
    [ProducesResponseType(typeof(BadgeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BadgeResponse>> SetBadge([FromBody] SetBadgeRequest request)
    {
        try
        {
            var playerId = GetPlayerId();
            var result = await _premiumService.SetBadgeAsync(playerId, request.Badge);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>List badges available for your tier.</summary>
    /// <remarks>
    /// See which badges you can equip based on your subscription tier.
    /// Upgrading to Premium Plus unlocks exclusive badges.
    /// </remarks>
    /// <response code="200">List of available badge names.</response>
    [ApiDifficulty("beginner")]
    [ApiPrerequisite("Register and login")]
    [HttpGet("badges")]
    [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<string>>> GetAvailableBadges()
    {
        var playerId = GetPlayerId();
        var badges = await _premiumService.GetAvailableBadgesAsync(playerId);
        return Ok(badges);
    }

    private Guid GetPlayerId()
    {
        var claim = User.FindFirst("PlayerId") ?? User.FindFirst(ClaimTypes.NameIdentifier);
        if (claim == null || !Guid.TryParse(claim.Value, out var playerId))
            throw new UnauthorizedAccessException("Invalid token.");
        return playerId;
    }
}

/// <summary>Request to set a profile badge.</summary>
public class SetBadgeRequest
{
    /// <summary>Badge name to equip, or null to remove.</summary>
    public string? Badge { get; set; }
}
