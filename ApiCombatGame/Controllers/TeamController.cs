using System.Security.Claims;
using System.Text.Json;
using ApiCombatGame.Data;
using ApiCombatGame.Filters.Attributes;
using ApiCombatGame.Models.Domain;
using ApiCombatGame.Models.DTOs.Common;
using ApiCombatGame.Models.DTOs.Strategy;
using ApiCombatGame.Models.DTOs.Team;
using ApiCombatGame.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiCombatGame.Controllers;

/// <summary>
/// Team configuration and management.
/// </summary>
[ApiCategoryMeta("users", "#06b6d4", Order = 3)]
[ApiController]
[Route("api/v1/team")]
[Authorize]
[Tags("Team")]
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

    /// <summary>Create a new team.</summary>
    /// <remarks>
    /// Assemble a squad of 1-5 units from your roster and optionally assign a battle strategy.
    /// Teams are your entry ticket to battle — you cannot queue without one.
    ///
    /// The strategy object controls how your units behave in combat: formation, target priority,
    /// and conditional ability usage. If omitted, units use default AI behavior.
    /// </remarks>
    /// <param name="request">Team name, unit IDs, and optional strategy configuration.</param>
    /// <response code="201">Team created with full unit details and strategy.</response>
    /// <response code="400">Invalid team size, missing units, or units not in your roster.</response>
    [ApiDifficulty("intermediate")]
    [ApiGameTip("Mix unit classes for a balanced team — a pure Warrior squad gets shredded by Mages. Include at least one Healer or Tank for survivability.")]
    [ApiGameTip("The strategy field is optional but powerful. Even a simple target-priority rule like 'focus healers first' can dramatically improve your win rate.")]
    [ApiPrerequisite("Register and login", "Unlock at least 1 unit")]
    [ApiExample("Create a balanced team with strategy", Request = "{\n  \"name\": \"Alpha Strike Force\",\n  \"unitIds\": [\n    \"d4c3b2a1-8f7e-6d5c-4b3a-2e1f0a9b8c7d\",\n    \"e5d4c3b2-9a8f-7e6d-5c4b-3a2e1f0a9b8c\",\n    \"f6e5d4c3-ab9a-8f7e-6d5c-4b3a2e1f0a9b\"\n  ],\n  \"strategy\": {\n    \"formation\": \"balanced\",\n    \"targetPriority\": [\"healers\", \"lowest_hp\"],\n    \"abilities\": {\n      \"Fireball\": {\n        \"when\": \"always\",\n        \"target\": \"priority\"\n      },\n      \"Fortify\": {\n        \"when\": \"ally_hp_below_50\",\n        \"target\": \"self\"\n      }\n    }\n  }\n}", Response = "{\n  \"id\": \"c8a7b6d5-4e3f-2a1b-9c8d-7e6f5a4b3c2d\",\n  \"name\": \"Alpha Strike Force\",\n  \"units\": [\n    {\n      \"id\": \"d4c3b2a1-8f7e-6d5c-4b3a-2e1f0a9b8c7d\",\n      \"name\": \"Iron Vanguard\",\n      \"class\": \"Warrior\",\n      \"level\": 1,\n      \"health\": 120,\n      \"attack\": 25,\n      \"defense\": 30,\n      \"speed\": 10\n    }\n  ],\n  \"strategy\": {\n    \"formation\": \"balanced\",\n    \"targetPriority\": [\"healers\", \"lowest_hp\"],\n    \"abilities\": {}\n  },\n  \"createdAt\": \"2026-02-17T14:30:00Z\",\n  \"updatedAt\": \"2026-02-17T14:30:00Z\"\n}")]
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

        // Enforce team slot limits based on subscription tier
        var player = await _context.Players.FindAsync(playerId);
        if (player == null) return NotFound(new { error = "Player not found." });

        var maxTeams = player.CurrentTier == SubscriptionTier.Free ? 3 : 10;
        var currentTeamCount = await _context.Teams.CountAsync(t => t.PlayerId == playerId);
        if (currentTeamCount >= maxTeams)
            return BadRequest(new
            {
                error = $"Team slot limit reached ({maxTeams} for {player.CurrentTier} tier). " +
                        (player.CurrentTier == SubscriptionTier.Free
                            ? "Upgrade to Premium for up to 10 team slots."
                            : "Maximum team slots reached."),
                maxTeams,
                currentTeams = currentTeamCount,
                tier = player.CurrentTier.ToString(),
                upgradeUrl = player.CurrentTier == SubscriptionTier.Free ? "/Account/Subscription" : (string?)null
            });

        // Verify all units belong to the player
        var playerUnits = await _context.Units
            .Where(u => u.PlayerId == playerId && !u.IsTemplate && request.UnitIds.Contains(u.Id))
            .ToListAsync();

        if (playerUnits.Count != request.UnitIds.Count)
            return BadRequest(new { error = "One or more units not found in your roster." });

        // Validate ability names in strategy match actual unit abilities
        if (request.Strategy?.Abilities.Any() == true)
        {
            var unitAbilityNames = await _context.Abilities
                .Where(a => request.UnitIds.Contains(a.UnitId))
                .Select(a => a.Name)
                .Distinct()
                .ToListAsync();

            var unknownAbilities = request.Strategy.Abilities.Keys
                .Where(k => !unitAbilityNames.Contains(k))
                .ToList();

            if (unknownAbilities.Count > 0)
                return BadRequest(new
                {
                    error = $"Strategy references abilities not on your units: {string.Join(", ", unknownAbilities)}. Available: {string.Join(", ", unitAbilityNames)}.",
                    unknownAbilities,
                    availableAbilities = unitAbilityNames
                });
        }

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
        response.Links = new Dictionary<string, ApiLink>
        {
            ["self"] = Links.Get($"/api/v1/team/{response.Id}"),
            ["queue_battle"] = Links.Post("/api/v1/battle/queue", "Queue a battle with this team"),
            ["delete"] = Links.Delete($"/api/v1/team/{response.Id}", "Delete this team")
        };
        return Created($"/api/v1/team/{team.Id}", response);
    }

    /// <summary>Get a specific team by ID.</summary>
    /// <param name="teamId">The unique team identifier.</param>
    /// <response code="200">Team details with units and strategy.</response>
    /// <response code="404">Team not found or does not belong to you.</response>
    [ApiDifficulty("beginner")]
    [ApiPrerequisite("Create a team")]
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

        var response = BuildTeamResponse(team, units);
        response.Links = new Dictionary<string, ApiLink>
        {
            ["self"] = Links.Get($"/api/v1/team/{response.Id}"),
            ["queue_battle"] = Links.Post("/api/v1/battle/queue", "Queue a battle with this team"),
            ["delete"] = Links.Delete($"/api/v1/team/{response.Id}", "Delete this team")
        };
        return Ok(response);
    }

    /// <summary>List all your teams.</summary>
    /// <remarks>
    /// Returns every team you have configured, each with full unit details and strategy.
    /// </remarks>
    /// <response code="200">Array of all your teams.</response>
    [ApiDifficulty("beginner")]
    [ApiPrerequisite("Create at least one team")]
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
            var r = BuildTeamResponse(t, teamUnits);
            r.Links = new Dictionary<string, ApiLink>
            {
                ["self"] = Links.Get($"/api/v1/team/{r.Id}"),
                ["queue_battle"] = Links.Post("/api/v1/battle/queue", "Queue a battle with this team"),
                ["delete"] = Links.Delete($"/api/v1/team/{r.Id}", "Delete this team")
            };
            return r;
        }).ToList();

        return Ok(responses);
    }

    /// <summary>Update an existing team.</summary>
    /// <remarks>
    /// Replace the team's name, unit composition, and/or strategy. Send the complete desired
    /// state — this is a full replacement, not a partial update.
    /// </remarks>
    /// <param name="teamId">The team to update.</param>
    /// <param name="request">Updated team configuration.</param>
    /// <response code="200">Updated team details.</response>
    /// <response code="400">Invalid configuration.</response>
    /// <response code="404">Team not found or does not belong to you.</response>
    [ApiDifficulty("intermediate")]
    [ApiGameTip("This is a full replacement, not a partial update — always send the complete team configuration including all unit IDs, even the ones that haven't changed.")]
    [ApiPrerequisite("Create a team")]
    [HttpPut("{teamId}")]
    [ProducesResponseType(typeof(TeamResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
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

        // Validate ability names in strategy match actual unit abilities
        if (request.Strategy?.Abilities.Any() == true)
        {
            var unitAbilityNames = await _context.Abilities
                .Where(a => request.UnitIds.Contains(a.UnitId))
                .Select(a => a.Name)
                .Distinct()
                .ToListAsync();

            var unknownAbilities = request.Strategy.Abilities.Keys
                .Where(k => !unitAbilityNames.Contains(k))
                .ToList();

            if (unknownAbilities.Count > 0)
                return BadRequest(new
                {
                    error = $"Strategy references abilities not on your units: {string.Join(", ", unknownAbilities)}. Available: {string.Join(", ", unitAbilityNames)}.",
                    unknownAbilities,
                    availableAbilities = unitAbilityNames
                });
        }

        team.Name = request.Name;
        team.UnitIdsJson = JsonSerializer.Serialize(request.UnitIds, JsonOptions);
        team.StrategyJson = request.Strategy != null
            ? JsonSerializer.Serialize(request.Strategy, JsonOptions)
            : team.StrategyJson;
        team.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var response = BuildTeamResponse(team, playerUnits);
        response.Links = new Dictionary<string, ApiLink>
        {
            ["self"] = Links.Get($"/api/v1/team/{response.Id}"),
            ["queue_battle"] = Links.Post("/api/v1/battle/queue", "Queue a battle with this team"),
            ["delete"] = Links.Delete($"/api/v1/team/{response.Id}", "Delete this team")
        };
        return Ok(response);
    }

    /// <summary>Delete a team.</summary>
    /// <remarks>
    /// Permanently removes a team configuration. The units themselves remain in your roster.
    /// </remarks>
    /// <param name="teamId">The team to delete.</param>
    /// <response code="204">Team deleted.</response>
    /// <response code="404">Team not found or does not belong to you.</response>
    [ApiDifficulty("beginner")]
    [ApiGameTip("Deleting a team only removes the configuration — all your units remain safely in your roster and can be reassigned to other teams.")]
    [ApiPrerequisite("Create a team")]
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
