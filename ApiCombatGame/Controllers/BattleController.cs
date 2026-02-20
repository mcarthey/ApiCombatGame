using System.Security.Claims;
using ApiCombatGame.Data;
using ApiCombatGame.Filters.Attributes;
using ApiCombatGame.Models.DTOs.Battle;
using ApiCombatGame.Models.DTOs.Common;
using ApiCombatGame.Models.Enums;
using ApiCombatGame.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiCombatGame.Controllers;

/// <summary>
/// Battle queue and results.
/// </summary>
[ApiCategoryMeta("swords", "#ef4444", Order = 4)]
[ApiController]
[Route("api/v1/battle")]
[Authorize]
[Tags("Battle")]
public class BattleController : ControllerBase
{
    private readonly IBattleService _battleService;
    private readonly GameDbContext _context;
    private readonly ILogger<BattleController> _logger;

    public BattleController(IBattleService battleService, GameDbContext context, ILogger<BattleController> logger)
    {
        _battleService = battleService;
        _context = context;
        _logger = logger;
    }

    /// <summary>Enter the battle queue.</summary>
    /// <remarks>
    /// Submit a team to the matchmaking queue. The system pairs you with an opponent of similar rating.
    /// Battles resolve asynchronously — poll the status endpoint, then fetch results.
    ///
    /// Flow: Queue → Poll status → Get results.
    /// </remarks>
    /// <param name="request">Team ID and battle mode.</param>
    /// <response code="201">Queued successfully. Poll the returned battle ID for status updates.</response>
    /// <response code="400">Team not found, already in queue, or daily battle limit reached.</response>
    [ApiDifficulty("intermediate")]
    [ApiGameTip("Always check the active modifiers via GET /api/v1/modifiers/active before queuing — a modifier that weakens your main unit class can cost you the match.")]
    [ApiGameTip("Use casual mode to test new strategies without risking your ranked rating. Switch to ranked once your win rate in casual exceeds 60%.")]
    [ApiPrerequisite("Register and login", "Build a team")]
    [ApiExample("Queue Ranked Battle", Request = "{\n  \"teamId\": \"a1b2c3d4-5678-9abc-def0-1234567890ab\",\n  \"mode\": \"ranked\"\n}", Response = "{\n  \"battleId\": \"f47ac10b-58cc-4372-a567-0e02b2c3d479\",\n  \"status\": \"Queued\",\n  \"queuedAt\": \"2026-02-11T14:30:00Z\",\n  \"estimatedWaitSeconds\": 12\n}")]
    [HttpPost("queue")]
    [ProducesResponseType(typeof(BattleStatusResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> QueueBattle([FromBody] BattleQueueRequest request)
    {
        try
        {
            var playerId = GetPlayerId();

            // Enforce daily battle limit for Free tier
            var player = await _context.Players.FindAsync(playerId);
            if (player == null) return NotFound(new { error = "Player not found." });

            if (player.CurrentTier == SubscriptionTier.Free)
            {
                // Reset counter if it's a new day
                if (player.LastBattleResetDate.Date < DateTime.UtcNow.Date)
                {
                    player.DailyBattlesUsed = 0;
                    player.LastBattleResetDate = DateTime.UtcNow.Date;
                    await _context.SaveChangesAsync();
                }

                if (player.DailyBattlesUsed >= 10)
                    return BadRequest(new
                    {
                        error = "Daily battle limit reached (10/day for Free tier). Upgrade to Premium for unlimited battles.",
                        dailyLimit = 10,
                        battlesUsed = player.DailyBattlesUsed,
                        resetsAt = DateTime.UtcNow.Date.AddDays(1).ToString("O"),
                        upgradeUrl = "/Account/Subscription"
                    });

                player.DailyBattlesUsed++;
                await _context.SaveChangesAsync();
            }

            var response = await _battleService.QueueBattleAsync(playerId, request);
            response.Links = new Dictionary<string, ApiLink>
            {
                ["self"] = Links.Get($"/api/v1/battle/status/{response.BattleId}"),
                ["results"] = Links.Get($"/api/v1/battle/results/{response.BattleId}", "Get results when completed")
            };
            return Created($"/api/v1/battle/status/{response.BattleId}", response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Check the status of a battle.</summary>
    /// <remarks>
    /// Poll this endpoint to track your battle from queue to completion.
    /// Status progresses: Queued → InProgress → Completed.
    /// </remarks>
    /// <param name="battleId">The battle to check.</param>
    /// <response code="200">Current battle status with timing information.</response>
    /// <response code="404">Battle not found.</response>
    [ApiDifficulty("beginner")]
    [ApiGameTip("Poll this endpoint every 3-5 seconds after queuing. Most battles resolve within 15 seconds, so aggressive polling just wastes your rate limit.")]
    [ApiPrerequisite("Queue a battle")]
    [HttpGet("status/{battleId}")]
    [ProducesResponseType(typeof(BattleStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBattleStatus(Guid battleId)
    {
        try
        {
            var playerId = GetPlayerId();
            var response = await _battleService.GetBattleStatusAsync(battleId, playerId);
            response.Links = new Dictionary<string, ApiLink>
            {
                ["self"] = Links.Get($"/api/v1/battle/status/{battleId}"),
                ["results"] = Links.Get($"/api/v1/battle/results/{battleId}", "Get results when completed")
            };
            return Ok(response);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>Get detailed results of a completed battle.</summary>
    /// <remarks>
    /// Returns the full battle outcome: winner, turn count, a turn-by-turn combat log,
    /// and rewards earned. The battle log contains every action taken by every unit on every
    /// turn — perfect for analyzing your strategy's effectiveness.
    /// </remarks>
    /// <param name="battleId">The completed battle.</param>
    /// <response code="200">Full battle results with combat log and rewards.</response>
    /// <response code="404">Battle not found or not yet completed.</response>
    [ApiDifficulty("beginner")]
    [ApiGameTip("Study the combat log turn by turn to see exactly where your units fell. Look for turns where your healer was targeted early — that usually signals a strategy gap you can fix with better positioning.")]
    [ApiPrerequisite("Queue a battle", "Wait for completion")]
    [HttpGet("results/{battleId}")]
    [ProducesResponseType(typeof(BattleResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBattleResults(Guid battleId)
    {
        try
        {
            var playerId = GetPlayerId();
            var response = await _battleService.GetBattleResultAsync(battleId, playerId);
            response.Links = new Dictionary<string, ApiLink>
            {
                ["self"] = Links.Get($"/api/v1/battle/results/{battleId}"),
                ["queue_again"] = Links.Post("/api/v1/battle/queue", "Queue another battle")
            };
            var replay = await _context.BattleReplays.FirstOrDefaultAsync(r => r.BattleId == battleId);
            if (replay != null)
                response.Links["replay"] = Links.Get($"/api/v1/replays/{replay.ShareUrl}", "Watch replay");
            else
                response.Links["create_replay"] = Links.Post("/api/v1/replays/create", "Create replay");
            if (response.WinnerId.HasValue)
                response.Links["winner"] = Links.Get($"/api/v1/leaderboard/player/{response.WinnerId}");
            if (response.LoserId.HasValue)
                response.Links["loser"] = Links.Get($"/api/v1/leaderboard/player/{response.LoserId}");
            return Ok(response);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>Get your recent battle history.</summary>
    /// <remarks>
    /// Returns your past battles in reverse chronological order with pagination support.
    /// </remarks>
    /// <param name="limit">Maximum battles to return (default 20).</param>
    /// <param name="offset">Number of battles to skip for pagination (default 0).</param>
    /// <response code="200">Array of battle results.</response>
    [ApiDifficulty("beginner")]
    [ApiGameTip("Pull your last 50 battles and tally losses by opponent unit class. If one class beats you consistently, adjust your team composition to add a counter before your next ranked session.")]
    [ApiPrerequisite("Complete at least one battle")]
    [HttpGet("history")]
    [ProducesResponseType(typeof(List<BattleResultResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBattleHistory([FromQuery] int limit = 20, [FromQuery] int offset = 0)
    {
        var playerId = GetPlayerId();
        limit = Math.Clamp(limit, 1, 100);
        offset = Math.Max(0, offset);

        var results = await _battleService.GetBattleHistoryAsync(playerId, limit, offset);
        var battleIds = results.Select(r => r.BattleId).ToList();
        var replays = await _context.BattleReplays
            .Where(r => battleIds.Contains(r.BattleId))
            .ToDictionaryAsync(r => r.BattleId, r => r.ShareUrl);

        foreach (var r in results)
        {
            r.Links = new Dictionary<string, ApiLink>
            {
                ["self"] = Links.Get($"/api/v1/battle/results/{r.BattleId}")
            };
            if (replays.TryGetValue(r.BattleId, out var shareUrl))
                r.Links["replay"] = Links.Get($"/api/v1/replays/{shareUrl}", "Watch replay");
        }
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
