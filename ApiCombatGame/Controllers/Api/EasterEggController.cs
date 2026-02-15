using System.Security.Claims;
using ApiCombatGame.Models.DTOs.EasterEgg;
using ApiCombatGame.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiCombatGame.Controllers.Api;

[ApiController]
[Route("api/eggs")]
[Authorize(AuthenticationSchemes = "Cookies,Bearer")]
public class EasterEggController : ControllerBase
{
    private readonly IEasterEggService _eggService;

    public EasterEggController(IEasterEggService eggService)
    {
        _eggService = eggService;
    }

    /// <summary>Check if there's a hidden egg on the current page.</summary>
    [HttpGet("check")]
    public async Task<ActionResult<EggCheckResponse>> Check([FromQuery] string page)
    {
        if (string.IsNullOrEmpty(page))
            return Ok(new EggCheckResponse { HasEgg = false });

        var egg = await _eggService.GetActiveEggForPageAsync(page);
        if (egg == null)
            return Ok(new EggCheckResponse { HasEgg = false });

        var playerId = GetPlayerId();
        var claimed = await _eggService.HasPlayerClaimedAsync(playerId, egg.Id);

        return Ok(new EggCheckResponse
        {
            HasEgg = true,
            Selector = egg.CssSelector,
            OffsetX = egg.OffsetX,
            OffsetY = egg.OffsetY,
            Icon = egg.IconName,
            Code = egg.Code,
            AlreadyClaimed = claimed
        });
    }

    /// <summary>Redeem an egg code to claim your loot reward.</summary>
    [HttpPost("redeem")]
    public async Task<ActionResult<EggRedeemResponse>> Redeem([FromBody] EggRedeemRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
            return BadRequest(new EggRedeemResponse { Success = false, Error = "Code is required." });

        var playerId = GetPlayerId();
        var claim = await _eggService.RedeemCodeAsync(playerId, request.Code.Trim().ToUpper());

        if (claim == null)
            return Ok(new EggRedeemResponse
            {
                Success = false,
                Error = "Invalid code, already claimed, or expired."
            });

        return Ok(new EggRedeemResponse
        {
            Success = true,
            LootType = claim.LootType,
            LootDescription = claim.LootDescription,
            LootAmount = claim.LootAmount,
            Code = request.Code.Trim().ToUpper()
        });
    }

    /// <summary>Get your egg hunt stats for the current week.</summary>
    [HttpGet("stats")]
    public async Task<ActionResult<EggHuntStatsResponse>> Stats()
    {
        var playerId = GetPlayerId();
        var stats = await _eggService.GetWeeklyStatsAsync(playerId);

        return Ok(new EggHuntStatsResponse
        {
            TotalEggsThisWeek = stats.TotalEggsThisWeek,
            EggsFoundThisWeek = stats.EggsFoundThisWeek,
            TotalLootEarned = stats.TotalLootEarned,
            AllTimeEggsFound = stats.AllTimeEggsFound
        });
    }

    private Guid GetPlayerId()
    {
        var claim = User.FindFirst("PlayerId") ?? User.FindFirst(ClaimTypes.NameIdentifier);
        return Guid.Parse(claim!.Value);
    }
}
