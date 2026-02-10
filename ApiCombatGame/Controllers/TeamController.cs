using System.Security.Claims;
using System.Text.Json;
using ApiCombatGame.Data;
using ApiCombatGame.Models.Domain;
using ApiCombatGame.Models.DTOs.Strategy;
using ApiCombatGame.Models.DTOs.Team;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiCombatGame.Controllers;

/// <summary>
/// Team configuration and management.
/// </summary>
[ApiController]
[Route("api/v1/team")]
[Authorize]
public class TeamController : ControllerBase
{
    private readonly GameDbContext _context;
    private readonly IConfiguration _config;
    private readonly ILogger<TeamController> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public TeamController(GameDbContext context, IConfiguration config, ILogger<TeamController> logger)
    {
        _context = context;
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Create a new team configuration.
    /// </summary>
    [HttpPost("configure")]
    [ProducesResponseType(typeof(TeamResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Configure([FromBody] TeamConfigRequest request)
    {
        var playerId = GetPlayerId();
        var maxTeamSize = _config.GetValue<int>("GameSettings:MaxTeamSize", 5);

        if (request.UnitIds.Count > maxTeamSize)
            return BadRequest(new { error = $"Team cannot have more than {maxTeamSize} units." });

        if (request.UnitIds.Count == 0)
            return BadRequest(new { error = "Team must have at least one unit." });

        // Verify all units belong to the player
        var playerUnits = await _context.Units
            .Where(u => u.PlayerId == playerId && !u.IsTemplate && request.UnitIds.Contains(u.Id))
            .ToListAsync();

        if (playerUnits.Count != request.UnitIds.Count)
            return BadRequest(new { error = "One or more units not found in your roster." });

        var team = new Team
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            PlayerId = playerId,
            UnitIdsJson = JsonSerializer.Serialize(request.UnitIds, JsonOptions),
            StrategyJson = request.Strategy != null
                ? JsonSerializer.Serialize(request.Strategy, JsonOptions)
                : "{}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Teams.Add(team);
        await _context.SaveChangesAsync();

        var response = BuildTeamResponse(team, playerUnits);
        return Created($"/api/v1/team/{team.Id}", response);
    }

    /// <summary>
    /// Get a specific team by ID.
    /// </summary>
    [HttpGet("{teamId}")]
    [ProducesResponseType(typeof(TeamResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTeam(Guid teamId)
    {
        var playerId = GetPlayerId();
        var team = await _context.Teams
            .FirstOrDefaultAsync(t => t.Id == teamId && t.PlayerId == playerId);

        if (team == null)
            return NotFound(new { error = "Team not found." });

        var unitIds = JsonSerializer.Deserialize<List<Guid>>(team.UnitIdsJson, JsonOptions) ?? new();
        var units = await _context.Units
            .Where(u => unitIds.Contains(u.Id))
            .ToListAsync();

        return Ok(BuildTeamResponse(team, units));
    }

    /// <summary>
    /// List all teams for the current player.
    /// </summary>
    [HttpGet("list")]
    [ProducesResponseType(typeof(List<TeamResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListTeams()
    {
        var playerId = GetPlayerId();
        var teams = await _context.Teams
            .Where(t => t.PlayerId == playerId)
            .ToListAsync();

        var allUnitIds = teams
            .SelectMany(t => JsonSerializer.Deserialize<List<Guid>>(t.UnitIdsJson, JsonOptions) ?? new())
            .Distinct()
            .ToList();

        var units = await _context.Units
            .Where(u => allUnitIds.Contains(u.Id))
            .ToListAsync();

        var responses = teams.Select(t =>
        {
            var teamUnitIds = JsonSerializer.Deserialize<List<Guid>>(t.UnitIdsJson, JsonOptions) ?? new();
            var teamUnits = units.Where(u => teamUnitIds.Contains(u.Id)).ToList();
            return BuildTeamResponse(t, teamUnits);
        }).ToList();

        return Ok(responses);
    }

    /// <summary>
    /// Update an existing team.
    /// </summary>
    [HttpPut("{teamId}")]
    [ProducesResponseType(typeof(TeamResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTeam(Guid teamId, [FromBody] TeamConfigRequest request)
    {
        var playerId = GetPlayerId();
        var maxTeamSize = _config.GetValue<int>("GameSettings:MaxTeamSize", 5);

        var team = await _context.Teams
            .FirstOrDefaultAsync(t => t.Id == teamId && t.PlayerId == playerId);

        if (team == null)
            return NotFound(new { error = "Team not found." });

        if (request.UnitIds.Count > maxTeamSize)
            return BadRequest(new { error = $"Team cannot have more than {maxTeamSize} units." });

        if (request.UnitIds.Count == 0)
            return BadRequest(new { error = "Team must have at least one unit." });

        var playerUnits = await _context.Units
            .Where(u => u.PlayerId == playerId && !u.IsTemplate && request.UnitIds.Contains(u.Id))
            .ToListAsync();

        if (playerUnits.Count != request.UnitIds.Count)
            return BadRequest(new { error = "One or more units not found in your roster." });

        team.Name = request.Name;
        team.UnitIdsJson = JsonSerializer.Serialize(request.UnitIds, JsonOptions);
        team.StrategyJson = request.Strategy != null
            ? JsonSerializer.Serialize(request.Strategy, JsonOptions)
            : team.StrategyJson;
        team.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(BuildTeamResponse(team, playerUnits));
    }

    /// <summary>
    /// Delete a team.
    /// </summary>
    [HttpDelete("{teamId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTeam(Guid teamId)
    {
        var playerId = GetPlayerId();
        var team = await _context.Teams
            .FirstOrDefaultAsync(t => t.Id == teamId && t.PlayerId == playerId);

        if (team == null)
            return NotFound(new { error = "Team not found." });

        _context.Teams.Remove(team);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private TeamResponse BuildTeamResponse(Team team, List<Unit> units)
    {
        StrategyConfig? strategy = null;
        if (!string.IsNullOrEmpty(team.StrategyJson) && team.StrategyJson != "{}")
        {
            try { strategy = JsonSerializer.Deserialize<StrategyConfig>(team.StrategyJson, JsonOptions); }
            catch { /* ignore parse errors */ }
        }

        return new TeamResponse
        {
            Id = team.Id,
            Name = team.Name,
            Units = units.Select(u => new UnitSummary
            {
                Id = u.Id,
                Name = u.Name,
                Class = u.Class.ToString(),
                Level = u.Level,
                Health = u.Health,
                Attack = u.Attack,
                Defense = u.Defense,
                Speed = u.Speed
            }).ToList(),
            Strategy = strategy,
            CreatedAt = team.CreatedAt,
            UpdatedAt = team.UpdatedAt
        };
    }

    private Guid GetPlayerId()
    {
        var claim = User.FindFirst("PlayerId") ?? User.FindFirst(ClaimTypes.NameIdentifier);
        if (claim == null || !Guid.TryParse(claim.Value, out var playerId))
            throw new UnauthorizedAccessException("Invalid token.");
        return playerId;
    }
}
