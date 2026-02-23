using System.Security.Claims;
using ApiCombatGame.Filters;
using ApiCombatGame.Filters.Attributes;
using ApiCombatGame.Models.DTOs.Guild;
using ApiCombatGame.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiCombatGame.Controllers.Api;

/// <summary>
/// Guild strategy library — share, browse, and manage battle strategies.
/// </summary>
[ApiController]
[Route("api/v1/guild")]
[Authorize]
[Tags("Guild")]
public class GuildStrategyController : ControllerBase
{
    private readonly IGuildStrategyService _guildStrategyService;

    public GuildStrategyController(IGuildStrategyService guildStrategyService)
    {
        _guildStrategyService = guildStrategyService;
    }

    /// <summary>List guild strategies.</summary>
    /// <remarks>
    /// View all battle strategies shared in the guild library. Strategies are sorted by usage count.
    /// </remarks>
    /// <param name="guildId">The guild to view strategies of.</param>
    /// <response code="200">Array of guild strategies.</response>
    [ApiDifficulty("beginner")]
    [ApiGameTip("Copy a high-usage strategy's JSON into your team configuration to benefit from proven tactics.")]
    [ApiPrerequisite("Join a guild")]
    [HttpGet("{guildId}/strategies")]
    [ProducesResponseType(typeof(List<GuildStrategyResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStrategies(Guid guildId)
    {
        var strategies = await _guildStrategyService.GetStrategiesAsync(guildId);
        return Ok(strategies.Select(s => new GuildStrategyResponse
        {
            Id = s.Id,
            Name = s.Name,
            Description = s.Description,
            CreatorUsername = s.Creator?.Username ?? "",
            Strategy = SafeDeserializeJson(s.StrategyJson),
            UsageCount = s.UsageCount,
            CreatedAt = s.CreatedAt,
            UpdatedAt = s.UpdatedAt
        }));
    }

    /// <summary>Publish a strategy to the guild library.</summary>
    /// <remarks>
    /// Share a battle strategy with your guild. Requires Officer or Leader role.
    /// </remarks>
    /// <param name="guildId">Your guild ID.</param>
    /// <param name="request">Strategy name, description, and configuration JSON.</param>
    /// <response code="201">Strategy published.</response>
    /// <response code="400">Insufficient permissions or invalid data.</response>
    [ApiDifficulty("intermediate")]
    [ApiGameTip("Include a detailed description explaining when and how to use the strategy — your guildmates will thank you.")]
    [ApiPrerequisite("Join a guild", "Officer or Leader role")]
    [HttpPost("{guildId}/strategies")]
    [ProducesResponseType(typeof(GuildStrategyResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PublishStrategy(Guid guildId, [FromBody] PublishGuildStrategyRequest request)
    {
        try
        {
            var playerId = GetPlayerId();
            var strategyJson = System.Text.Json.JsonSerializer.Serialize(request.Strategy);
            var strategy = await _guildStrategyService.PublishAsync(guildId, playerId, request.Name, request.Description, strategyJson);

            return Created($"/api/v1/guild/{guildId}/strategies/{strategy.Id}", new GuildStrategyResponse
            {
                Id = strategy.Id,
                Name = strategy.Name,
                Description = strategy.Description,
                CreatorUsername = strategy.Creator?.Username ?? "",
                Strategy = request.Strategy,
                UsageCount = 0,
                CreatedAt = strategy.CreatedAt,
                UpdatedAt = strategy.UpdatedAt
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Update a guild strategy.</summary>
    /// <remarks>
    /// Modify a strategy in the guild library. Only the original creator or the guild leader can update.
    /// </remarks>
    /// <param name="guildId">Your guild ID.</param>
    /// <param name="strategyId">The strategy to update.</param>
    /// <param name="request">Fields to update (all optional).</param>
    /// <response code="200">Strategy updated.</response>
    /// <response code="400">Insufficient permissions.</response>
    /// <response code="404">Strategy not found.</response>
    [ApiDifficulty("intermediate")]
    [ApiPrerequisite("Publish a strategy")]
    [HttpPut("{guildId}/strategies/{strategyId}")]
    [ProducesResponseType(typeof(GuildStrategyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStrategy(Guid guildId, Guid strategyId, [FromBody] UpdateGuildStrategyRequest request)
    {
        try
        {
            var playerId = GetPlayerId();
            var strategyJson = request.Strategy != null ? System.Text.Json.JsonSerializer.Serialize(request.Strategy) : null;
            var strategy = await _guildStrategyService.UpdateAsync(guildId, strategyId, playerId, request.Name, request.Description, strategyJson);

            return Ok(new GuildStrategyResponse
            {
                Id = strategy.Id,
                Name = strategy.Name,
                Description = strategy.Description,
                CreatorUsername = strategy.Creator?.Username ?? "",
                Strategy = System.Text.Json.JsonSerializer.Deserialize<object>(strategy.StrategyJson),
                UsageCount = strategy.UsageCount,
                CreatedAt = strategy.CreatedAt,
                UpdatedAt = strategy.UpdatedAt
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

    /// <summary>Delete a guild strategy.</summary>
    /// <remarks>
    /// Remove a strategy from the guild library. Only the original creator or guild leader can delete.
    /// </remarks>
    /// <param name="guildId">Your guild ID.</param>
    /// <param name="strategyId">The strategy to delete.</param>
    /// <response code="204">Strategy deleted.</response>
    /// <response code="400">Insufficient permissions.</response>
    /// <response code="404">Strategy not found.</response>
    [ApiDifficulty("intermediate")]
    [ApiPrerequisite("Publish a strategy")]
    [HttpDelete("{guildId}/strategies/{strategyId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteStrategy(Guid guildId, Guid strategyId)
    {
        try
        {
            var playerId = GetPlayerId();
            await _guildStrategyService.DeleteAsync(guildId, strategyId, playerId);
            return NoContent();
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

    private Guid GetPlayerId()
    {
        var claim = User.FindFirst("PlayerId") ?? User.FindFirst(ClaimTypes.NameIdentifier);
        if (claim == null || !Guid.TryParse(claim.Value, out var playerId))
            throw new UnauthorizedAccessException("Invalid token.");
        return playerId;
    }

    private static object? SafeDeserializeJson(string? json)
    {
        if (string.IsNullOrEmpty(json)) return null;
        try { return System.Text.Json.JsonSerializer.Deserialize<object>(json); }
        catch (System.Text.Json.JsonException) { return null; }
    }
}
