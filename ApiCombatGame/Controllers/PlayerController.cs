using System.Security.Claims;
using System.Text.Json;
using ApiCombatGame.Data;
using ApiCombatGame.Filters.Attributes;
using ApiCombatGame.Models.Domain;
using ApiCombatGame.Models.DTOs.Common;
using ApiCombatGame.Models.DTOs.Team;
using ApiCombatGame.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiCombatGame.Controllers;

/// <summary>
/// Player profile and roster management.
/// </summary>
[ApiCategoryMeta("user", "#8b5cf6", Order = 2)]
[ApiController]
[Route("api/v1/player")]
[Authorize]
[Tags("Player")]
public class PlayerController : ControllerBase
{
    private readonly GameDbContext _context;
    private readonly IPlayerProgressionService _progressionService;
    private readonly IAchievementService _achievementService;
    private readonly INotificationService _notifications;
    private readonly ILogger<PlayerController> _logger;

    public PlayerController(
        GameDbContext context,
        IPlayerProgressionService progressionService,
        IAchievementService achievementService,
        INotificationService notifications,
        ILogger<PlayerController> logger)
    {
        _context = context;
        _progressionService = progressionService;
        _achievementService = achievementService;
        _notifications = notifications;
        _logger = logger;
    }

    /// <summary>Get your player profile.</summary>
    /// <remarks>
    /// Returns your account details including level, currency balance, API rating,
    /// and counts of owned units and configured teams.
    /// </remarks>
    /// <response code="200">Player profile with current stats.</response>
    /// <response code="404">Player not found.</response>
    [ApiDifficulty("beginner")]
    [ApiGameTip("Always check your currency balance here before attempting to unlock new units — insufficient funds will result in a 400 error.")]
    [ApiPrerequisite("Register and login")]
    [HttpGet("profile")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfile()
    {
        var playerId = GetPlayerId();
        var player = await _context.Players.FindAsync(playerId);
        if (player == null) return NotFound(new { error = "Player not found." });

        // Guild info
        var guildMembership = await _context.GuildMemberships
            .Include(m => m.Guild)
            .FirstOrDefaultAsync(m => m.PlayerId == playerId);

        var achievementCount = await _context.PlayerAchievements
            .CountAsync(pa => pa.PlayerId == playerId && pa.IsUnlocked);

        return Ok(new
        {
            player.Id,
            player.Username,
            player.Email,
            player.Level,
            player.ExperiencePoints,
            XpToNextLevel = _progressionService.GetXpForLevel(player.Level),
            player.Currency,
            player.Rating,
            player.WinStreak,
            Tier = player.CurrentTier.ToString(),
            player.AchievementPoints,
            AchievementsUnlocked = achievementCount,
            Guild = guildMembership != null ? new
            {
                GuildId = guildMembership.GuildId,
                Name = guildMembership.Guild.Name,
                Tag = guildMembership.Guild.Tag,
                Role = guildMembership.Role.ToString(),
                ContributionPoints = guildMembership.ContributionPoints
            } : null,
            player.CreatedAt,
            player.LastLoginAt,
            RosterCount = await _context.Units.CountAsync(u => u.PlayerId == playerId && !u.IsTemplate),
            TeamCount = await _context.Teams.CountAsync(t => t.PlayerId == playerId),
            _links = new Dictionary<string, ApiLink>
            {
                ["self"] = Links.Get("/api/v1/player/profile"),
                ["roster"] = Links.Get("/api/v1/player/roster", "Your owned units"),
                ["roster_available"] = Links.Get("/api/v1/player/roster/available", "Units available to unlock"),
                ["teams"] = Links.Get("/api/v1/player/teams", "Your configured teams"),
                ["achievements"] = Links.Get("/api/v1/player/achievements", "Your achievements"),
                ["notifications"] = Links.Get("/api/v1/player/notifications", "Your notifications"),
                ["leaderboard"] = Links.Get($"/api/v1/leaderboard/player/{playerId}", "Your leaderboard rank"),
                ["queue_battle"] = Links.Post("/api/v1/battle/queue", "Enter the arena")
            }
        });
    }

    /// <summary>List all units in your roster.</summary>
    /// <remarks>
    /// Returns every unit you own, including their stats, class, and abilities.
    /// These are the combatants available for team composition.
    /// </remarks>
    /// <response code="200">Array of owned units with full ability details.</response>
    [ApiDifficulty("beginner")]
    [ApiGameTip("Review each unit's abilities carefully before building teams — knowing cooldown timers and AoE vs single-target can make or break your strategy.")]
    [ApiPrerequisite("Register and login")]
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

    /// <summary>Unlock a new unit from the template roster.</summary>
    /// <remarks>
    /// Spend in-game currency to add a template unit to your roster. Each unit can only
    /// be unlocked once. Check available units and costs at GET /api/v1/player/roster/available.
    /// </remarks>
    /// <param name="request">The template unit to unlock.</param>
    /// <response code="201">Unit unlocked and added to your roster.</response>
    /// <response code="400">Insufficient currency or unit already owned.</response>
    [ApiDifficulty("beginner")]
    [ApiGameTip("Think rock-paper-scissors: Warriors beat Rogues, Rogues beat Mages, Mages beat Warriors. Choose units that complement each other to cover all matchups.")]
    [ApiPrerequisite("Register and login", "Browse available units")]
    [ApiExample("Unlock a Warrior unit", Request = "{\n  \"templateUnitId\": \"b7e4f2a1-9c38-4d56-a1e2-3f4b5c6d7e8f\"\n}", Response = "{\n  \"id\": \"d4c3b2a1-8f7e-6d5c-4b3a-2e1f0a9b8c7d\",\n  \"name\": \"Iron Vanguard\",\n  \"class\": \"Warrior\",\n  \"health\": 120,\n  \"attack\": 25,\n  \"defense\": 30,\n  \"speed\": 10,\n  \"currencyRemaining\": 700\n}")]
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

        // Achievement: unit unlocked
        await _achievementService.CheckAndAwardAsync(playerId, "unit_unlocked");

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

    /// <summary>Browse the unit shop.</summary>
    /// <remarks>
    /// Lists all template units available for purchase, with stats, class, abilities,
    /// and unlock cost. Units you already own are excluded.
    /// </remarks>
    /// <response code="200">Array of template units available for unlock.</response>
    [ApiDifficulty("beginner")]
    [ApiGameTip("The class system (Warrior, Rogue, Mage, Healer, Tank) has built-in strengths and weaknesses — study the stats before spending currency.")]
    [ApiPrerequisite("Register and login")]
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

    /// <summary>Get your achievements.</summary>
    /// <remarks>
    /// Returns all achievements with your progress toward each one.
    /// Secret achievements are hidden until unlocked.
    /// </remarks>
    /// <response code="200">Array of achievements with progress.</response>
    [ApiDifficulty("beginner")]
    [ApiGameTip("Focus on combat achievements first — they reward gold and XP that accelerate your progression.")]
    [ApiPrerequisite("Register and login")]
    [HttpGet("achievements")]
    [ProducesResponseType(typeof(List<Models.DTOs.Progression.AchievementResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAchievements()
    {
        var playerId = GetPlayerId();
        var achievements = await _context.Achievements.ToListAsync();
        var playerAchievements = await _context.PlayerAchievements
            .Where(pa => pa.PlayerId == playerId)
            .ToListAsync();

        var result = achievements.Select(a =>
        {
            var pa = playerAchievements.FirstOrDefault(p => p.AchievementId == a.Id);
            return new Models.DTOs.Progression.AchievementResponse
            {
                AchievementId = a.Id,
                Name = a.IsSecret && pa?.IsUnlocked != true ? "???" : a.Name,
                Description = a.IsSecret && pa?.IsUnlocked != true ? "Secret achievement — keep playing to discover it!" : a.Description,
                IconUrl = a.IconUrl,
                Category = a.Category,
                Points = a.Points,
                Progress = pa?.Progress ?? 0,
                RequiredProgress = pa?.RequiredProgress ?? 1,
                IsUnlocked = pa?.IsUnlocked ?? false,
                IsSecret = a.IsSecret,
                UnlockedAt = pa?.UnlockedAt
            };
        }).ToList();

        return Ok(result);
    }

    // ── Notifications ──

    /// <summary>Get your unread notification count.</summary>
    /// <response code="200">Unread count.</response>
    [HttpGet("notifications/count")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNotificationCount()
    {
        var playerId = GetPlayerId();
        var count = await _notifications.GetUnreadCountAsync(playerId);
        return Ok(new { unreadCount = count });
    }

    /// <summary>Get your notifications.</summary>
    /// <param name="page">Page number (default 1).</param>
    /// <param name="unreadOnly">Filter to unread only (default false).</param>
    /// <response code="200">Paginated notifications.</response>
    [HttpGet("notifications")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNotifications([FromQuery] int page = 1, [FromQuery] bool unreadOnly = false)
    {
        var playerId = GetPlayerId();
        var notifications = await _notifications.GetNotificationsAsync(playerId, page, 20, unreadOnly);
        var unreadCount = await _notifications.GetUnreadCountAsync(playerId);

        return Ok(new
        {
            unreadCount,
            page,
            notifications = notifications.Select(n => new
            {
                n.Id,
                Type = n.Type.ToString(),
                Category = n.Category.ToString(),
                n.Title,
                n.Message,
                n.ActionUrl,
                n.IsRead,
                n.CreatedAt
            })
        });
    }

    /// <summary>Mark a notification as read.</summary>
    /// <param name="notificationId">Notification ID.</param>
    /// <response code="204">Notification marked as read.</response>
    [HttpPost("notifications/{notificationId}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkNotificationRead(Guid notificationId)
    {
        var playerId = GetPlayerId();
        await _notifications.MarkReadAsync(notificationId, playerId);
        return NoContent();
    }

    /// <summary>Mark all notifications as read.</summary>
    /// <response code="204">All notifications marked as read.</response>
    [HttpPost("notifications/read-all")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkAllNotificationsRead()
    {
        var playerId = GetPlayerId();
        await _notifications.MarkAllReadAsync(playerId);
        return NoContent();
    }

    /// <summary>Get your notification preferences.</summary>
    /// <response code="200">Current notification preferences.</response>
    [HttpGet("notifications/preferences")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNotificationPreferences()
    {
        var playerId = GetPlayerId();
        var prefs = await _notifications.GetPreferencesAsync(playerId);
        return Ok(prefs);
    }

    /// <summary>Update your notification preferences.</summary>
    /// <response code="204">Preferences updated.</response>
    [HttpPut("notifications/preferences")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateNotificationPreferences([FromBody] NotificationPreferences preferences)
    {
        var playerId = GetPlayerId();
        await _notifications.UpdatePreferencesAsync(playerId, preferences);
        return NoContent();
    }

    /// <summary>Get notification digest (summary grouped by category).</summary>
    /// <remarks>
    /// Returns a compact digest of all unread notifications grouped by category,
    /// with counts and the 10 most recent highlights. Use this for a quick status check
    /// instead of paginating through all notifications.
    /// </remarks>
    /// <response code="200">Notification digest with category counts and highlights.</response>
    [ApiDifficulty("beginner")]
    [ApiGameTip("Check the digest endpoint for a quick overview before diving into individual notifications.")]
    [ApiPrerequisite("Register and login")]
    [HttpGet("notifications/digest")]
    [ProducesResponseType(typeof(NotificationDigestResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<NotificationDigestResponse>> GetNotificationDigest()
    {
        var playerId = GetPlayerId();
        var digest = await _notifications.GetDigestAsync(playerId);
        return Ok(digest);
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
