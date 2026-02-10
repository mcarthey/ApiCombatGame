using System.Security.Claims;
using ApiCombatGame.Models.DTOs.Battle;
using ApiCombatGame.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiCombatGame.Controllers;

/// <summary>
/// Battle queue and results.
/// </summary>
[ApiController]
[Route("api/v1/battle")]
[Authorize]
public class BattleController : ControllerBase
{
    private readonly IBattleService _battleService;
    private readonly ILogger<BattleController> _logger;

    public BattleController(IBattleService battleService, ILogger<BattleController> logger)
    {
        _battleService = battleService;
        _logger = logger;
    }

    /// <summary>
    /// Queue for a battle with a specific team.
    /// </summary>
    [HttpPost("queue")]
    [ProducesResponseType(typeof(BattleStatusResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> QueueBattle([FromBody] BattleQueueRequest request)
    {
        try
        {
            var playerId = GetPlayerId();
            var response = await _battleService.QueueBattleAsync(playerId, request);
            return Created($"/api/v1/battle/status/{response.BattleId}", response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get the current status of a battle.
    /// </summary>
    [HttpGet("status/{battleId}")]
    [ProducesResponseType(typeof(BattleStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBattleStatus(Guid battleId)
    {
        try
        {
            var playerId = GetPlayerId();
            var response = await _battleService.GetBattleStatusAsync(battleId, playerId);
            return Ok(response);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get detailed results of a completed battle.
    /// </summary>
    [HttpGet("results/{battleId}")]
    [ProducesResponseType(typeof(BattleResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBattleResults(Guid battleId)
    {
        try
        {
            var playerId = GetPlayerId();
            var response = await _battleService.GetBattleResultAsync(battleId, playerId);
            return Ok(response);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get battle history for the current player.
    /// </summary>
    [HttpGet("history")]
    [ProducesResponseType(typeof(List<BattleResultResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBattleHistory([FromQuery] int limit = 20, [FromQuery] int offset = 0)
    {
        var playerId = GetPlayerId();
        limit = Math.Clamp(limit, 1, 100);
        offset = Math.Max(0, offset);

        var results = await _battleService.GetBattleHistoryAsync(playerId, limit, offset);
        return Ok(results);
    }

    private Guid GetPlayerId()
    {
        var claim = User.FindFirst("PlayerId") ?? User.FindFirst(ClaimTypes.NameIdentifier);
        if (claim == null || !Guid.TryParse(claim.Value, out var playerId))
            throw new UnauthorizedAccessException("Invalid token.");
        return playerId;
    }
}
