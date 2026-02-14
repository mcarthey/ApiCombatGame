using System.Security.Claims;
using ApiCombatGame.Filters.Attributes;
using ApiCombatGame.Models.DTOs.Rival;
using ApiCombatGame.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiCombatGame.Controllers;

/// <summary>
/// Auto-assigned rivals for competitive motivation.
/// </summary>
[ApiCategoryMeta("target", "#dc2626", Order = 18)]
[ApiController]
[Route("api/v1/rival")]
[Authorize]
[Tags("Rival")]
public class RivalController : ControllerBase
{
    private readonly IRivalService _rivalService;

    public RivalController(IRivalService rivalService)
    {
        _rivalService = rivalService;
    }

    /// <summary>Get your current rival.</summary>
    /// <remarks>
    /// Every player is automatically assigned a rival — someone at a similar rating level.
    /// Rivals rotate weekly. When you beat your rival in a ranked battle, you earn a +100g bonus
    /// on top of normal rewards.
    ///
    /// The rival system creates a personal nemesis to drive competitive motivation. Track your
    /// head-to-head record and prove you're the better player.
    /// </remarks>
    /// <response code="200">Your current rival info, or hasRival=false if no suitable rival found.</response>
    [ApiDifficulty("beginner")]
    [ApiGameTip("Your rival is matched by rating, so they're always a fair challenge. Beating them proves you deserve your rank — and the 100g bonus doesn't hurt either.")]
    [ApiGameTip("Rivals rotate weekly. If you get a tough rival, focus on other battles. If you get an easy one, farm those bonus wins!")]
    [ApiPrerequisite("Register and login")]
    [ApiExample("Get Rival", Response = "{\n  \"rivalId\": \"...\",\n  \"rivalUsername\": \"ShadowBlade42\",\n  \"rivalRating\": 1050,\n  \"rivalLevel\": 5,\n  \"winsAgainstRival\": 2,\n  \"lossesAgainstRival\": 1,\n  \"bonusGoldEarned\": 200,\n  \"bonusPerWin\": 100,\n  \"hasRival\": true\n}")]
    [HttpGet("current")]
    [ProducesResponseType(typeof(RivalInfoResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRival()
    {
        var playerId = GetPlayerId();
        var result = await _rivalService.GetRivalAsync(playerId);
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
