using System.Security.Claims;
using ApiCombatGame.Filters.Attributes;
using ApiCombatGame.Models.DTOs.Progression;
using ApiCombatGame.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiCombatGame.Controllers.Api;

/// <summary>
/// Battle replay sharing system.
/// </summary>
[ApiController]
[Tags("Replays")]
[Route("api/v1/replays")]
[ApiCategoryMeta("film", "#64748b", Order = 11)]
public class ReplayController : ControllerBase
{
    private readonly IReplayService _replayService;

    public ReplayController(IReplayService replayService)
    {
        _replayService = replayService;
    }

    /// <summary>Create a shareable battle replay.</summary>
    /// <remarks>
    /// Generate a shareable link for a completed battle. The replay can be viewed by anyone
    /// with the link, even without an account. You must be a participant in the battle.
    /// </remarks>
    /// <param name="request">The battle ID to create a replay for.</param>
    /// <response code="201">Replay created with shareable URL.</response>
    /// <response code="400">Battle not completed or replay already exists.</response>
    /// <response code="404">Battle not found or you are not a participant.</response>
    [ApiDifficulty("beginner")]
    [ApiGameTip("Share your best victories with the community to build your reputation as a top strategist.")]
    [ApiPrerequisite("Complete a battle")]
    [ApiExample("Create a replay", Request = "{\n  \"battleId\": \"b7e8f9a0-1234-5678-9abc-def012345678\"\n}", Response = "{\n  \"replayId\": \"r1a2b3c4-d5e6-7890-abcd-ef1234567890\",\n  \"battleId\": \"b7e8f9a0-1234-5678-9abc-def012345678\",\n  \"shareUrl\": \"xK9mPq2v\",\n  \"viewCount\": 0,\n  \"createdAt\": \"2026-02-11T16:45:00Z\"\n}")]
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

    /// <summary>View a battle replay.</summary>
    /// <remarks>
    /// Retrieve a battle replay by its share URL. No authentication required — anyone can
    /// view shared replays. Each view increments the replay's view counter.
    /// </remarks>
    /// <param name="shareUrl">The unique share URL identifier.</param>
    /// <response code="200">Replay details with player names and view count.</response>
    /// <response code="404">Replay not found or share URL is invalid.</response>
    [ApiDifficulty("beginner")]
    [ApiGameTip("Study replays of your defeats to identify weaknesses and improve your battle strategies.")]
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
