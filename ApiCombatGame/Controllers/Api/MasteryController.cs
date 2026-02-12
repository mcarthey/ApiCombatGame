using System.Security.Claims;
using ApiCombatGame.Filters.Attributes;
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
[Tags("Mastery")]
[ApiCategoryMeta("star", "#a855f7", Order = 9)]
public class MasteryController : ControllerBase
{
    private readonly IMasteryService _masteryService;

    public MasteryController(IMasteryService masteryService)
    {
        _masteryService = masteryService;
    }

    /// <summary>Get mastery levels for all your units.</summary>
    /// <remarks>
    /// Returns mastery progression for every unit you have used in battle.
    /// Mastery increases with use — tracks battles fought, wins, and win rate per unit.
    /// </remarks>
    /// <response code="200">Array of mastery records for all units with battle experience.</response>
    [ApiDifficulty("beginner")]
    [ApiGameTip("Focus on mastering the units you use most often for faster progression and stronger teams.")]
    [ApiPrerequisite("Register and login", "Complete at least one battle")]
    [ApiExample("Get all unit mastery", Response = "[\n  {\n    \"unitId\": \"u1a2b3c4-d5e6-7890-abcd-ef1234567890\",\n    \"level\": 5,\n    \"experiencePoints\": 2450,\n    \"battlesUsed\": 47,\n    \"winsWithUnit\": 31\n  },\n  {\n    \"unitId\": \"u9f8e7d6-c5b4-3210-fedc-ba0987654321\",\n    \"level\": 3,\n    \"experiencePoints\": 1100,\n    \"battlesUsed\": 22,\n    \"winsWithUnit\": 12\n  }\n]")]
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

    /// <summary>Get mastery for a specific unit.</summary>
    /// <remarks>
    /// Returns detailed mastery data for one unit. If you have never used the unit
    /// in battle, returns a default record with Level 1 and zero experience.
    /// </remarks>
    /// <param name="unitId">The unit to check mastery for.</param>
    /// <response code="200">Unit mastery details including level, XP, battles, and win rate.</response>
    [ApiDifficulty("beginner")]
    [ApiGameTip("Win rate is the key driver of mastery growth -- winning battles grants more XP than losing.")]
    [ApiPrerequisite("Register and login")]
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
