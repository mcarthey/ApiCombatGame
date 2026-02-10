using System.Security.Claims;
using System.Text.Json;
using ApiCombatGame.Data;
using ApiCombatGame.Models.Domain;
using ApiCombatGame.Models.DTOs.Team;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiCombatGame.Controllers;

/// <summary>
/// Player profile and roster management.
/// </summary>
[ApiController]
[Route("api/v1/player")]
[Authorize]
public class PlayerController : ControllerBase
{
    private readonly GameDbContext _context;
    private readonly ILogger<PlayerController> _logger;

    public PlayerController(GameDbContext context, ILogger<PlayerController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get the current player's profile.
    /// </summary>
    [HttpGet("profile")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetProfile()
    {
        var playerId = GetPlayerId();
        var player = await _context.Players.FindAsync(playerId);
        if (player == null) return NotFound(new { error = "Player not found." });

        return Ok(new
        {
            player.Id,
            player.Username,
            player.Email,
            player.Level,
            player.Currency,
            player.Rating,
            player.CreatedAt,
            player.LastLoginAt,
            RosterCount = await _context.Units.CountAsync(u => u.PlayerId == playerId && !u.IsTemplate),
            TeamCount = await _context.Teams.CountAsync(t => t.PlayerId == playerId)
        });
    }

    /// <summary>
    /// Get the current player's unit roster.
    /// </summary>
    [HttpGet("roster")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRoster()
    {
        var playerId = GetPlayerId();
        var units = await _context.Units
            .Include(u => u.Abilities)
            .Where(u => u.PlayerId == playerId && !u.IsTemplate)
            .ToListAsync();

        return Ok(units.Select(u => new
        {
            u.Id,
            u.Name,
            Class = u.Class.ToString(),
            u.Level,
            u.Health,
            u.Attack,
            u.Defense,
            u.Speed,
            Abilities = u.Abilities.Select(a => new
            {
                a.Id,
                a.Name,
                Type = a.Type.ToString(),
                a.Damage,
                a.Healing,
                a.CooldownTurns,
                a.Description,
                a.TargetsAllies,
                a.IsAoE
            })
        }));
    }

    /// <summary>
    /// Unlock a new unit from the template roster using currency.
    /// </summary>
    [HttpPost("roster/unlock")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UnlockUnit([FromBody] UnlockUnitRequest request)
    {
        var playerId = GetPlayerId();
        var player = await _context.Players.FindAsync(playerId);
        if (player == null) return NotFound(new { error = "Player not found." });

        var template = await _context.Units
            .Include(u => u.Abilities)
            .FirstOrDefaultAsync(u => u.Id == request.TemplateUnitId && u.IsTemplate);

        if (template == null)
            return NotFound(new { error = "Template unit not found." });

        // Check if player already owns this unit type
        var alreadyOwned = await _context.Units
            .AnyAsync(u => u.PlayerId == playerId && u.Name == template.Name && !u.IsTemplate);
        if (alreadyOwned)
            return BadRequest(new { error = "You already own this unit." });

        if (player.Currency < template.UnlockCost)
            return BadRequest(new { error = $"Insufficient currency. Need {template.UnlockCost}, have {player.Currency}." });

        // Deduct currency and create unit copy
        player.Currency -= template.UnlockCost;

        var newUnit = new Unit
        {
            Id = Guid.NewGuid(),
            Name = template.Name,
            Class = template.Class,
            Level = 1,
            Health = template.Health,
            Attack = template.Attack,
            Defense = template.Defense,
            Speed = template.Speed,
            UnlockCost = 0,
            IsTemplate = false,
            PlayerId = playerId
        };

        var newAbilities = template.Abilities.Select(a => new Ability
        {
            Id = Guid.NewGuid(),
            UnitId = newUnit.Id,
            Name = a.Name,
            Type = a.Type,
            Damage = a.Damage,
            Healing = a.Healing,
            CooldownTurns = a.CooldownTurns,
            Description = a.Description,
            TargetsAllies = a.TargetsAllies,
            IsAoE = a.IsAoE
        }).ToList();

        _context.Units.Add(newUnit);
        _context.Abilities.AddRange(newAbilities);
        await _context.SaveChangesAsync();

        return Created($"/api/v1/player/roster", new
        {
            newUnit.Id,
            newUnit.Name,
            Class = newUnit.Class.ToString(),
            newUnit.Health,
            newUnit.Attack,
            newUnit.Defense,
            newUnit.Speed,
            CurrencyRemaining = player.Currency
        });
    }

    /// <summary>
    /// Get available template units that can be unlocked.
    /// </summary>
    [HttpGet("roster/available")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAvailableUnits()
    {
        var playerId = GetPlayerId();
        var ownedNames = await _context.Units
            .Where(u => u.PlayerId == playerId && !u.IsTemplate)
            .Select(u => u.Name)
            .ToListAsync();

        var templates = await _context.Units
            .Where(u => u.IsTemplate)
            .ToListAsync();

        return Ok(templates.Select(t => new
        {
            t.Id,
            t.Name,
            Class = t.Class.ToString(),
            t.Health,
            t.Attack,
            t.Defense,
            t.Speed,
            t.UnlockCost,
            AlreadyOwned = ownedNames.Contains(t.Name)
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

public class UnlockUnitRequest
{
    public Guid TemplateUnitId { get; set; }
}
