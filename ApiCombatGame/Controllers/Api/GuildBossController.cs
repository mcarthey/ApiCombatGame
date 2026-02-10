using System.Security.Claims;
using ApiCombatGame.Data;
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
public class GuildBossController : ControllerBase
{
    private readonly IGuildBossService _guildBossService;
    private readonly GameDbContext _context;

    public GuildBossController(IGuildBossService guildBossService, GameDbContext context)
    {
        _guildBossService = guildBossService;
        _context = context;
    }

    /// <summary>
    /// Get current guild boss.
    /// </summary>
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

    /// <summary>
    /// Attempt to damage the guild boss with your team.
    /// </summary>
    [HttpPost("attempt")]
    [ProducesResponseType(typeof(BossAttemptResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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

    /// <summary>
    /// Get boss damage leaderboard.
    /// </summary>
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
