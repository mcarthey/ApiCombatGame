using ApiCombatGame.Data;
using ApiCombatGame.Filters.Attributes;
using ApiCombatGame.Models.DTOs.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiCombatGame.Controllers;

/// <summary>
/// Leaderboard endpoints for player rankings.
/// </summary>
[ApiCategoryMeta("trophy", "#f59e0b", Order = 5)]
[ApiController]
[Route("api/v1/leaderboard")]
[Authorize]
[Tags("Leaderboard")]
public class LeaderboardController : ControllerBase
{
    private readonly GameDbContext _context;

    public LeaderboardController(GameDbContext context)
    {
        _context = context;
    }

    /// <summary>Get the global leaderboard.</summary>
    /// <remarks>
    /// Returns the top players ranked by API (Arena Power Index) rating with win/loss statistics.
    /// </remarks>
    /// <param name="limit">Number of top players to return (default 100).</param>
    /// <response code="200">Ranked array of top players with stats.</response>
    [ApiDifficulty("beginner")]
    [ApiGameTip("Fetch the top 10 and inspect their win rates. Players above 70% win rate often rely on specific unit-class synergies you can reverse-engineer by reviewing their public battle replays.")]
    [ApiPrerequisite("None — public endpoint")]
    [AllowAnonymous]
    [HttpGet]
    [ResponseCache(Duration = 30)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLeaderboard([FromQuery] int limit = 100)
    {
        limit = Math.Clamp(limit, 1, 100);

        var players = await _context.Players
            .Where(p => !p.IsBot && !p.IsDeleted)
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
            WinRate = p.TotalBattles > 0 ? Math.Round((double)p.WinCount / p.TotalBattles * 100, 1) : 0,
            _links = new Dictionary<string, ApiLink>
            {
                ["player"] = Links.Get($"/api/v1/leaderboard/player/{p.Id}")
            }
        });

        return Ok(ranked);
    }

    /// <summary>Get a specific player's ranking.</summary>
    /// <remarks>
    /// Look up any player's global rank, rating, and battle statistics.
    /// </remarks>
    /// <param name="playerId">The player to look up.</param>
    /// <response code="200">Player's rank, rating, and combat statistics.</response>
    /// <response code="404">Player not found.</response>
    [ApiDifficulty("beginner")]
    [ApiGameTip("Compare your win rate against the top 10 players periodically. Once you break 50% against the upper bracket, you are ready to push for a higher rank in competitive seasons.")]
    [ApiPrerequisite("None — public endpoint")]
    [AllowAnonymous]
    [HttpGet("player/{playerId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPlayerRanking(Guid playerId)
    {
        var player = await _context.Players.FindAsync(playerId);
        if (player == null || player.IsBot || player.IsDeleted)
            return NotFound(new { error = "Player not found." });

        var rank = await _context.Players
            .CountAsync(p => !p.IsBot && !p.IsDeleted && p.Rating > player.Rating) + 1;

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
            WinRate = totalBattles > 0 ? Math.Round((double)winCount / totalBattles * 100, 1) : 0,
            _links = new Dictionary<string, ApiLink>
            {
                ["self"] = Links.Get($"/api/v1/leaderboard/player/{playerId}"),
                ["profile"] = Links.Get("/api/v1/player/profile")
            }
        });
    }
}
