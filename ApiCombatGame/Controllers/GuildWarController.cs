using System.Security.Claims;
using ApiCombatGame.Filters.Attributes;
using ApiCombatGame.Models.DTOs.GuildWar;
using ApiCombatGame.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiCombatGame.Controllers;

/// <summary>
/// Guild vs Guild weekly wars. Your ranked wins earn points for your guild.
/// </summary>
[ApiCategoryMeta("swords", "#dc2626", Order = 20)]
[ApiController]
[Route("api/v1/guildwar")]
[Authorize]
[Tags("Guild Wars")]
public class GuildWarController : ControllerBase
{
    private readonly IGuildWarService _guildWarService;

    public GuildWarController(IGuildWarService guildWarService)
    {
        _guildWarService = guildWarService;
    }

    /// <summary>Get your guild's current war status.</summary>
    /// <remarks>
    /// Shows your guild's active war matchup, including scores, opponent info, and
    /// top contributors. Wars are matched weekly between guilds of similar level.
    ///
    /// Every ranked battle win during a war earns 10 points for your guild. The winning
    /// guild gets a treasury bonus at the end of the week. In a draw, the reward is split.
    /// </remarks>
    /// <response code="200">Current war status, or isAtWar=false if not matched.</response>
    [ApiDifficulty("beginner")]
    [ApiGameTip("Your ranked wins automatically contribute to guild wars — just play normally and you'll help your guild!")]
    [ApiGameTip("Check the top contributors list to see who's carrying your guild this week.")]
    [ApiPrerequisite("Join a guild with 3+ members")]
    [ApiExample("Get War Status", Response = "{\n  \"isAtWar\": true,\n  \"warId\": \"...\",\n  \"yourGuild\": \"Shadow Legion\",\n  \"yourScore\": 150,\n  \"opponentGuild\": \"Storm Riders\",\n  \"opponentScore\": 120,\n  \"daysRemaining\": 4,\n  \"treasuryReward\": 750,\n  \"topContributors\": [...]\n}")]
    [HttpGet("status")]
    [ProducesResponseType(typeof(GuildWarStatusResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWarStatus()
    {
        var playerId = GetPlayerId();
        var result = await _guildWarService.GetCurrentWarAsync(playerId);
        return Ok(result);
    }

    /// <summary>Get your guild's war history.</summary>
    /// <remarks>
    /// Returns past guild wars with results. Track your guild's competitive record
    /// and see how much treasury gold you've earned from victories.
    /// </remarks>
    /// <param name="limit">Maximum results to return (default 10).</param>
    /// <response code="200">List of past wars with scores and results.</response>
    [ApiDifficulty("beginner")]
    [ApiGameTip("Review past wars to see your guild's strength — are you winning more than losing?")]
    [ApiPrerequisite("Join a guild")]
    [HttpGet("history")]
    [ProducesResponseType(typeof(List<GuildWarHistoryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWarHistory([FromQuery] int limit = 10)
    {
        var playerId = GetPlayerId();
        limit = Math.Clamp(limit, 1, 50);
        var result = await _guildWarService.GetWarHistoryAsync(playerId, limit);
        return Ok(result);
    }

    private Guid GetPlayerId()
    {
        var claim = User.FindFirst("PlayerId") ?? User.FindFirst(ClaimTypes.NameIdentifier);
        if (claim == null || !Guid.TryParse(claim.Value, out var playerId))
            throw new UnauthorizedAccessException("Invalid token.");
        return playerId;
    }
}
