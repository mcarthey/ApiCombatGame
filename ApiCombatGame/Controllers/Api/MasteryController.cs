using System.Security.Claims;
using ApiCombatGame.Models.DTOs.Progression;
using ApiCombatGame.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiCombatGame.Controllers.Api;

/// <summary>
/// Unit mastery progression system.
/// </summary>
[ApiController]
[Route("api/v1/mastery")]
[Authorize]
public class MasteryController : ControllerBase
{
    private readonly IMasteryService _masteryService;

    public MasteryController(IMasteryService masteryService)
    {
        _masteryService = masteryService;
    }

    /// <summary>
    /// Get all unit mastery levels for the current player.
    /// </summary>
    [HttpGet("units")]
    [ProducesResponseType(typeof(List<MasteryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<MasteryResponse>>> GetMastery()
    {
        var playerId = GetPlayerId();
        var mastery = await _masteryService.GetPlayerMastery(playerId);

        return Ok(mastery.Select(m => new MasteryResponse
        {
            UnitId = m.UnitId,
            Level = m.Level,
            ExperiencePoints = m.ExperiencePoints,
            BattlesUsed = m.BattlesUsed,
            WinsWithUnit = m.WinsWithUnit
        }));
    }

    /// <summary>
    /// Get mastery for a specific unit.
    /// </summary>
    [HttpGet("unit/{unitId}")]
    [ProducesResponseType(typeof(MasteryResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<MasteryResponse>> GetUnitMastery(Guid unitId)
    {
        var playerId = GetPlayerId();
        var mastery = await _masteryService.GetUnitMastery(playerId, unitId);

        if (mastery == null)
        {
            return Ok(new MasteryResponse
            {
                UnitId = unitId,
                Level = 1,
                ExperiencePoints = 0,
                BattlesUsed = 0,
                WinsWithUnit = 0
            });
        }

        return Ok(new MasteryResponse
        {
            UnitId = mastery.UnitId,
            Level = mastery.Level,
            ExperiencePoints = mastery.ExperiencePoints,
            BattlesUsed = mastery.BattlesUsed,
            WinsWithUnit = mastery.WinsWithUnit
        });
    }

    private Guid GetPlayerId()
    {
        var claim = User.FindFirst("PlayerId") ?? User.FindFirst(ClaimTypes.NameIdentifier);
        if (claim == null || !Guid.TryParse(claim.Value, out var playerId))
            throw new UnauthorizedAccessException("Invalid token.");
        return playerId;
    }
}
