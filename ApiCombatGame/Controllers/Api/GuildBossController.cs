using System.Security.Claims;
using ApiCombatGame.Data;
using ApiCombatGame.Filters.Attributes;
using ApiCombatGame.Models.DTOs.Guild;
using ApiCombatGame.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiCombatGame.Controllers.Api;

/// <summary>
/// Guild boss raid system.
/// </summary>
[ApiController]
[Route("api/v1/guild/boss")]
[Authorize]
[Tags("Guild Boss")]
[ApiCategoryMeta("dragon", "#dc2626", Order = 8)]
public class GuildBossController : ControllerBase
{
    private readonly IGuildBossService _guildBossService;
    private readonly GameDbContext _context;

    public GuildBossController(IGuildBossService guildBossService, GameDbContext context)
    {
        _guildBossService = guildBossService;
        _context = context;
    }

    /// <summary>Get your guild's current boss encounter.</summary>
    /// <remarks>
    /// Returns the active raid boss including remaining HP, expiration time, and rewards.
    /// Bosses spawn periodically and must be defeated before they expire.
    /// You must be a member of a guild to access boss encounters.
    /// </remarks>
    /// <response code="200">Active boss details with HP, rewards, and expiration.</response>
    /// <response code="404">Not in a guild, or no active boss encounter.</response>
    [ApiDifficulty("intermediate")]
    [ApiGameTip("Coordinate attack timing with your guildmates to chain damage before the boss expires.")]
    [ApiPrerequisite("Register and login", "Join a guild")]
    [ApiExample("View current boss", Response = "{\n  \"bossId\": \"b0a1c2d3-e4f5-6789-0abc-def123456789\",\n  \"name\": \"Shadow Dragon Kaelthos\",\n  \"description\": \"A fearsome dragon that deals AoE fire damage\",\n  \"maxHp\": 50000,\n  \"currentHp\": 32150,\n  \"expiresAt\": \"2026-02-12T18:00:00Z\",\n  \"isDefeated\": false,\n  \"rewardCurrency\": 1000,\n  \"rewardExperience\": 2500\n}")]
    [HttpGet("current")]
    [ProducesResponseType(typeof(GuildBossResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GuildBossResponse>> GetCurrentBoss()
    {
        var playerId = GetPlayerId();

        var membership = await _context.GuildMemberships
            .FirstOrDefaultAsync(m => m.PlayerId == playerId);

        if (membership == null)
            return NotFound(new { error = "You are not in a guild" });

        var boss = await _guildBossService.GetActiveGuildBoss(membership.GuildId);
        if (boss == null)
            return NotFound(new { error = "No active boss for your guild" });

        return Ok(new GuildBossResponse
        {
            BossId = boss.Id,
            Name = boss.Name,
            Description = boss.Description,
            MaxHp = boss.MaxHp,
            CurrentHp = boss.CurrentHp,
            ExpiresAt = boss.ExpiresAt,
            IsDefeated = boss.IsDefeated,
            RewardCurrency = boss.RewardCurrency,
            RewardExperience = boss.RewardExperience
        });
    }

    /// <summary>Attack the guild boss with your team.</summary>
    /// <remarks>
    /// Send a team to damage the guild boss. Multiple guild members can attack the same boss —
    /// coordinate for maximum impact. If your attack delivers the killing blow,
    /// wasKillingBlow will be true and bonus rewards may be awarded.
    /// </remarks>
    /// <param name="request">Boss ID and the team to send into the raid.</param>
    /// <response code="200">Attack result with damage dealt.</response>
    /// <response code="400">Team not found or boss already defeated.</response>
    /// <response code="404">Boss not found or not in a guild.</response>
    /// <response code="501">Boss battle resolution is in preview and not yet available.</response>
    [ApiDifficulty("advanced")]
    [ApiGameTip("Send your strongest team composition for maximum damage output against the boss.")]
    [ApiGameTip("Landing the killing blow grants bonus currency and experience rewards to that player.")]
    [ApiPrerequisite("Join a guild", "Build a team")]
    [ApiExample("Attack the boss", Request = "{\n  \"bossId\": \"b0a1c2d3-e4f5-6789-0abc-def123456789\",\n  \"teamId\": \"d4e5f6a7-b8c9-0123-4567-890abcdef012\"\n}", Response = "{\n  \"attemptId\": \"a1234567-b89c-def0-1234-567890abcdef\",\n  \"damageDealt\": 4250,\n  \"wasKillingBlow\": false,\n  \"attemptedAt\": \"2026-02-11T15:22:00Z\"\n}")]
    [HttpPost("attempt")]
    [ProducesResponseType(typeof(BossAttemptResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status501NotImplemented)]
    public async Task<ActionResult<BossAttemptResponse>> AttemptBoss([FromBody] BossAttemptRequest request)
    {
        try
        {
            var playerId = GetPlayerId();
            var attempt = await _guildBossService.AttemptBoss(request.BossId, playerId, request.TeamId);

            return Ok(new BossAttemptResponse
            {
                AttemptId = attempt.Id,
                DamageDealt = attempt.DamageDealt,
                WasKillingBlow = attempt.WasKillingBlow,
                AttemptedAt = attempt.AttemptedAt
            });
        }
        catch (NotImplementedException)
        {
            return StatusCode(501, new { error = "Boss battle logic not yet implemented" });
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

    /// <summary>Get the boss damage leaderboard.</summary>
    /// <remarks>
    /// See which guild members dealt the most damage to a specific boss encounter.
    /// </remarks>
    /// <param name="bossId">The boss encounter to view rankings for.</param>
    /// <response code="200">Damage attempts ranked by damage dealt.</response>
    [ApiDifficulty("beginner")]
    [ApiPrerequisite("Join a guild")]
    [HttpGet("leaderboard")]
    [ProducesResponseType(typeof(List<BossAttemptResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<BossAttemptResponse>>> GetLeaderboard([FromQuery] Guid bossId)
    {
        var attempts = await _guildBossService.GetBossLeaderboard(bossId);

        return Ok(attempts.Select(a => new BossAttemptResponse
        {
            AttemptId = a.Id,
            PlayerName = a.Player.Username,
            DamageDealt = a.DamageDealt,
            WasKillingBlow = a.WasKillingBlow,
            AttemptedAt = a.AttemptedAt
        }));
    }

    private Guid GetPlayerId()
    {
        var claim = User.FindFirst("PlayerId") ?? User.FindFirst(ClaimTypes.NameIdentifier);
        if (claim == null || !Guid.TryParse(claim.Value, out var playerId))
            throw new UnauthorizedAccessException("Invalid token.");
        return playerId;
    }
}
