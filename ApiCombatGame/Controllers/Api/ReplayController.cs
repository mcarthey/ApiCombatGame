using System.Security.Claims;
using ApiCombatGame.Models.DTOs.Progression;
using ApiCombatGame.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiCombatGame.Controllers.Api;

/// <summary>
/// Battle replay sharing system.
/// </summary>
[ApiController]
[Route("api/v1/replays")]
public class ReplayController : ControllerBase
{
    private readonly IReplayService _replayService;

    public ReplayController(IReplayService replayService)
    {
        _replayService = replayService;
    }

    /// <summary>
    /// Create a shareable replay for a completed battle.
    /// </summary>
    [HttpPost("create")]
    [Authorize]
    [ProducesResponseType(typeof(ReplayResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReplayResponse>> CreateReplay([FromBody] CreateReplayRequest request)
    {
        try
        {
            var replay = await _replayService.CreateReplay(request.BattleId);

            return Created($"/api/v1/replays/{replay.ShareUrl}", new ReplayResponse
            {
                ReplayId = replay.Id,
                BattleId = replay.BattleId,
                ShareUrl = replay.ShareUrl,
                ViewCount = replay.ViewCount,
                CreatedAt = replay.CreatedAt
            });
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

    /// <summary>
    /// Get a battle replay by share URL (public, no auth required).
    /// </summary>
    [HttpGet("{shareUrl}")]
    [ProducesResponseType(typeof(ReplayResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReplayResponse>> GetReplay(string shareUrl)
    {
        var replay = await _replayService.GetReplay(shareUrl);
        if (replay == null)
        {
            return NotFound(new { error = "Replay not found" });
        }

        await _replayService.IncrementViewCount(replay.Id);

        return Ok(new ReplayResponse
        {
            ReplayId = replay.Id,
            BattleId = replay.BattleId,
            ShareUrl = replay.ShareUrl,
            ViewCount = replay.ViewCount + 1,
            IsFeatured = replay.IsFeatured,
            CreatedAt = replay.CreatedAt,
            Player1Name = replay.Battle?.Player1?.Username ?? "Unknown",
            Player2Name = replay.Battle?.Player2?.Username ?? "Unknown"
        });
    }
}
