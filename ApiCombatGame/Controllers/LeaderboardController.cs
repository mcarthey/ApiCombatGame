using ApiCombatGame.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiCombatGame.Controllers;

/// <summary>
/// Leaderboard endpoints for player rankings.
/// </summary>
[ApiController]
[Route("api/v1/leaderboard")]
[Authorize]
public class LeaderboardController : ControllerBase
{
    private readonly GameDbContext _context;

    public LeaderboardController(GameDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Get the top players by rating.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLeaderboard([FromQuery] int limit = 100)
    {
        limit = Math.Clamp(limit, 1, 500);

        var players = await _context.Players
            .OrderByDescending(p => p.Rating)
            .Take(limit)
            .Select(p => new
            {
                p.Id,
                p.Username,
                p.Rating,
                p.Level,
                WinCount = _context.Battles.Count(b => b.WinnerId == p.Id),
                TotalBattles = _context.Battles.Count(b =>
                    (b.Player1Id == p.Id || b.Player2Id == p.Id) &&
                    b.Status == Models.Enums.BattleStatus.Completed)
            })
            .ToListAsync();

        var ranked = players.Select((p, index) => new
        {
            Rank = index + 1,
            p.Id,
            p.Username,
            p.Rating,
            p.Level,
            p.WinCount,
            p.TotalBattles,
            WinRate = p.TotalBattles > 0 ? Math.Round((double)p.WinCount / p.TotalBattles * 100, 1) : 0
        });

        return Ok(ranked);
    }

    /// <summary>
    /// Get a specific player's leaderboard position.
    /// </summary>
    [HttpGet("player/{playerId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPlayerRanking(Guid playerId)
    {
        var player = await _context.Players.FindAsync(playerId);
        if (player == null)
            return NotFound(new { error = "Player not found." });

        var rank = await _context.Players
            .CountAsync(p => p.Rating > player.Rating) + 1;

        var winCount = await _context.Battles.CountAsync(b => b.WinnerId == playerId);
        var totalBattles = await _context.Battles.CountAsync(b =>
            (b.Player1Id == playerId || b.Player2Id == playerId) &&
            b.Status == Models.Enums.BattleStatus.Completed);

        return Ok(new
        {
            Rank = rank,
            player.Id,
            player.Username,
            player.Rating,
            player.Level,
            WinCount = winCount,
            TotalBattles = totalBattles,
            WinRate = totalBattles > 0 ? Math.Round((double)winCount / totalBattles * 100, 1) : 0
        });
    }
}
