using System.Security.Claims;
using ApiCombatGame.Filters;
using ApiCombatGame.Filters.Attributes;
using ApiCombatGame.Models.DTOs.Common;
using ApiCombatGame.Models.DTOs.Guild;
using ApiCombatGame.Models.Enums;
using ApiCombatGame.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiCombatGame.Controllers.Api;

/// <summary>
/// Guild management — create, invite, promote, and coordinate.
/// </summary>
[ApiController]
[Route("api/v1/guild")]
[Authorize]
[Tags("Guild")]
[ApiCategoryMeta("shield", "#f59e0b", Order = 7)]
public class GuildController : ControllerBase
{
    private readonly IGuildService _guildService;

    public GuildController(IGuildService guildService)
    {
        _guildService = guildService;
    }

    /// <summary>Create a new guild.</summary>
    /// <remarks>
    /// Founds a new guild with you as the leader. Requires Premium tier or higher.
    /// Guild names and tags must be unique. Tags are automatically uppercased.
    /// You cannot create a guild if you're already in one.
    /// </remarks>
    /// <param name="request">Guild name, tag (3-5 chars), and description.</param>
    /// <response code="201">Guild created. You are now the leader.</response>
    /// <response code="400">Already in a guild, name/tag taken, or insufficient tier.</response>
    [ApiDifficulty("intermediate")]
    [ApiGameTip("Guild creation requires Premium tier. Once created, your guild starts with 20 member slots — purchase upgrades from the treasury to expand.")]
    [ApiPrerequisite("Register and login", "Premium subscription")]
    [ApiExample("Create a guild", Request = "{\n  \"name\": \"Shadow Wolves\",\n  \"tag\": \"SWLF\",\n  \"description\": \"Elite PvP guild focused on ranked battles\"\n}", Response = "{\n  \"guildId\": \"a1b2c3d4-e5f6-7890-abcd-ef1234567890\",\n  \"name\": \"Shadow Wolves\",\n  \"tag\": \"SWLF\",\n  \"description\": \"Elite PvP guild focused on ranked battles\",\n  \"leaderName\": \"YourUsername\",\n  \"level\": 1,\n  \"memberCount\": 1,\n  \"maxMembers\": 20,\n  \"createdAt\": \"2026-02-11T14:00:00Z\"\n}")]
    [RequiresTier(SubscriptionTier.Premium)]
    [HttpPost("create")]
    [ProducesResponseType(typeof(GuildResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateGuild([FromBody] CreateGuildRequest request)
    {
        try
        {
            var playerId = GetPlayerId();
            var guild = await _guildService.CreateGuildAsync(playerId, request.Name, request.Tag, request.Description);

            var response = new GuildResponse
            {
                GuildId = guild.Id,
                Name = guild.Name,
                Tag = guild.Tag,
                Description = guild.Description,
                LeaderName = (await _guildService.GetGuildAsync(guild.Id))?.Leader?.Username ?? "",
                Level = guild.Level,
                MemberCount = 1,
                MaxMembers = guild.MaxMembers,
                CreatedAt = guild.CreatedAt
            };
            response.Links = new Dictionary<string, ApiLink>
            {
                ["self"] = Links.Get($"/api/v1/guild/{response.GuildId}"),
                ["members"] = Links.Get($"/api/v1/guild/{response.GuildId}/members"),
                ["boss"] = Links.Get("/api/v1/guild/boss/current"),
                ["treasury"] = Links.Get($"/api/v1/guild/{response.GuildId}/treasury"),
                ["strategies"] = Links.Get($"/api/v1/guild/{response.GuildId}/strategies")
            };

            return Created($"/api/v1/guild/{guild.Id}", response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Delete your guild.</summary>
    /// <remarks>
    /// Permanently deletes the guild and removes all members. Only the guild leader can perform this action.
    /// This action is irreversible.
    /// </remarks>
    /// <param name="guildId">The guild to delete.</param>
    /// <response code="204">Guild deleted.</response>
    /// <response code="400">You are not the leader.</response>
    /// <response code="404">Guild not found.</response>
    [ApiDifficulty("intermediate")]
    [ApiGameTip("This action is irreversible and removes all members. Consider transferring leadership instead.")]
    [ApiPrerequisite("Create a guild")]
    [HttpDelete("{guildId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteGuild(Guid guildId)
    {
        try
        {
            var playerId = GetPlayerId();
            await _guildService.DeleteGuildAsync(guildId, playerId);
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

    /// <summary>Get guild details.</summary>
    /// <param name="guildId">The guild to view.</param>
    /// <response code="200">Guild info with member count and level.</response>
    /// <response code="404">Guild not found.</response>
    [ApiDifficulty("beginner")]
    [ApiPrerequisite("Register and login")]
    [HttpGet("{guildId}")]
    [ProducesResponseType(typeof(GuildResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetGuild(Guid guildId)
    {
        var guild = await _guildService.GetGuildAsync(guildId);
        if (guild == null) return NotFound(new { error = "Guild not found." });

        var response = new GuildResponse
        {
            GuildId = guild.Id,
            Name = guild.Name,
            Tag = guild.Tag,
            Description = guild.Description,
            LeaderName = guild.Leader?.Username ?? "",
            Level = guild.Level,
            MemberCount = guild.Members.Count,
            MaxMembers = guild.MaxMembers,
            CreatedAt = guild.CreatedAt
        };
        response.Links = new Dictionary<string, ApiLink>
        {
            ["self"] = Links.Get($"/api/v1/guild/{response.GuildId}"),
            ["members"] = Links.Get($"/api/v1/guild/{response.GuildId}/members"),
            ["boss"] = Links.Get("/api/v1/guild/boss/current"),
            ["treasury"] = Links.Get($"/api/v1/guild/{response.GuildId}/treasury"),
            ["strategies"] = Links.Get($"/api/v1/guild/{response.GuildId}/strategies")
        };
        return Ok(response);
    }

    /// <summary>Get your current guild.</summary>
    /// <remarks>
    /// Returns guild details for the guild you currently belong to.
    /// </remarks>
    /// <response code="200">Your guild info.</response>
    /// <response code="404">You are not in a guild.</response>
    [ApiDifficulty("beginner")]
    [ApiPrerequisite("Join a guild")]
    [HttpGet("mine")]
    [ProducesResponseType(typeof(GuildResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyGuild()
    {
        var playerId = GetPlayerId();
        var guild = await _guildService.GetPlayerGuildAsync(playerId);
        if (guild == null) return NotFound(new { error = "You are not in a guild." });

        var response = new GuildResponse
        {
            GuildId = guild.Id,
            Name = guild.Name,
            Tag = guild.Tag,
            Description = guild.Description,
            LeaderName = guild.Leader?.Username ?? "",
            Level = guild.Level,
            MemberCount = guild.Members.Count,
            MaxMembers = guild.MaxMembers,
            CreatedAt = guild.CreatedAt
        };
        response.Links = new Dictionary<string, ApiLink>
        {
            ["self"] = Links.Get($"/api/v1/guild/{response.GuildId}"),
            ["members"] = Links.Get($"/api/v1/guild/{response.GuildId}/members"),
            ["boss"] = Links.Get("/api/v1/guild/boss/current"),
            ["treasury"] = Links.Get($"/api/v1/guild/{response.GuildId}/treasury"),
            ["strategies"] = Links.Get($"/api/v1/guild/{response.GuildId}/strategies")
        };
        return Ok(response);
    }

    /// <summary>List guild members.</summary>
    /// <param name="guildId">The guild to view members of.</param>
    /// <response code="200">Array of guild members with roles and contribution points.</response>
    /// <response code="404">Guild not found.</response>
    [ApiDifficulty("beginner")]
    [ApiPrerequisite("Register and login")]
    [HttpGet("{guildId}/members")]
    [ProducesResponseType(typeof(List<GuildMemberDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMembers(Guid guildId)
    {
        var guild = await _guildService.GetGuildAsync(guildId);
        if (guild == null) return NotFound(new { error = "Guild not found." });

        var members = await _guildService.GetMembersAsync(guildId);

        return Ok(members.Select(m => new GuildMemberDto
        {
            PlayerId = m.PlayerId,
            Username = m.Player.Username,
            Role = m.Role.ToString(),
            JoinedAt = m.JoinedAt,
            ContributionPoints = m.ContributionPoints,
            Rating = m.Player.Rating,
            Level = m.Player.Level
        }));
    }

    /// <summary>Invite a player to your guild.</summary>
    /// <remarks>
    /// Send a guild invitation to another player by username. Requires Officer or Leader role.
    /// Invites expire after 7 days. The target player must not already be in a guild.
    /// </remarks>
    /// <param name="guildId">Your guild ID.</param>
    /// <param name="request">Username of the player to invite.</param>
    /// <response code="201">Invite sent.</response>
    /// <response code="400">Player already in a guild, already invited, or insufficient permissions.</response>
    /// <response code="404">Player not found.</response>
    [ApiDifficulty("intermediate")]
    [ApiGameTip("Invites expire after 7 days. If the player doesn't respond, you can send another invite once it expires.")]
    [ApiPrerequisite("Join a guild", "Officer or Leader role")]
    [ApiExample("Invite a player", Request = "{\n  \"username\": \"EliteWarrior42\"\n}", Response = "{\n  \"inviteId\": \"f47ac10b-58cc-4372-a567-0e02b2c3d479\",\n  \"guildId\": \"a1b2c3d4-e5f6-7890-abcd-ef1234567890\",\n  \"guildName\": \"Shadow Wolves\",\n  \"guildTag\": \"SWLF\",\n  \"invitedByUsername\": \"YourUsername\",\n  \"status\": \"Pending\",\n  \"createdAt\": \"2026-02-11T14:30:00Z\",\n  \"expiresAt\": \"2026-02-18T14:30:00Z\"\n}")]
    [HttpPost("{guildId}/invite")]
    [ProducesResponseType(typeof(GuildInviteResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> InvitePlayer(Guid guildId, [FromBody] InvitePlayerRequest request)
    {
        try
        {
            var playerId = GetPlayerId();
            var invite = await _guildService.InvitePlayerAsync(guildId, playerId, request.Username);

            var guild = await _guildService.GetGuildAsync(guildId);
            var inviter = guild?.Members.FirstOrDefault(m => m.PlayerId == playerId);

            return Created($"/api/v1/guild/invites/{invite.Id}", new GuildInviteResponse
            {
                InviteId = invite.Id,
                GuildId = guildId,
                GuildName = guild?.Name ?? "",
                GuildTag = guild?.Tag ?? "",
                InvitedByUsername = inviter?.Player?.Username ?? "",
                Status = invite.Status.ToString(),
                CreatedAt = invite.CreatedAt,
                ExpiresAt = invite.ExpiresAt
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

    /// <summary>View your pending guild invites.</summary>
    /// <response code="200">Array of pending invites.</response>
    [ApiDifficulty("beginner")]
    [ApiPrerequisite("Register and login")]
    [HttpGet("invites")]
    [ProducesResponseType(typeof(List<GuildInviteResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPendingInvites()
    {
        var playerId = GetPlayerId();
        var invites = await _guildService.GetPendingInvitesAsync(playerId);

        return Ok(invites.Select(i => new GuildInviteResponse
        {
            InviteId = i.Id,
            GuildId = i.GuildId,
            GuildName = i.Guild?.Name ?? "",
            GuildTag = i.Guild?.Tag ?? "",
            InvitedByUsername = i.InvitedBy?.Username ?? "",
            Status = i.Status.ToString(),
            CreatedAt = i.CreatedAt,
            ExpiresAt = i.ExpiresAt
        }));
    }

    /// <summary>Accept a guild invite.</summary>
    /// <param name="inviteId">The invite to accept.</param>
    /// <response code="200">Joined the guild.</response>
    /// <response code="400">Invite expired, already in a guild, or guild is full.</response>
    /// <response code="404">Invite not found.</response>
    [ApiDifficulty("beginner")]
    [ApiGameTip("Accepting an invite automatically declines all your other pending invites.")]
    [ApiPrerequisite("Receive a guild invite")]
    [HttpPost("invites/{inviteId}/accept")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AcceptInvite(Guid inviteId)
    {
        try
        {
            var playerId = GetPlayerId();
            var invite = await _guildService.AcceptInviteAsync(inviteId, playerId);
            return Ok(new { message = "You have joined the guild.", guildId = invite.GuildId });
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

    /// <summary>Decline a guild invite.</summary>
    /// <param name="inviteId">The invite to decline.</param>
    /// <response code="200">Invite declined.</response>
    /// <response code="400">Invite already responded to.</response>
    /// <response code="404">Invite not found.</response>
    [ApiDifficulty("beginner")]
    [ApiPrerequisite("Receive a guild invite")]
    [HttpPost("invites/{inviteId}/decline")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeclineInvite(Guid inviteId)
    {
        try
        {
            var playerId = GetPlayerId();
            await _guildService.DeclineInviteAsync(inviteId, playerId);
            return Ok(new { message = "Invite declined." });
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

    /// <summary>Kick a member from the guild.</summary>
    /// <remarks>
    /// Remove a player from your guild. Requires Leader role. You cannot kick members
    /// with equal or higher rank than yourself.
    /// </remarks>
    /// <param name="guildId">Your guild ID.</param>
    /// <param name="request">The player to kick.</param>
    /// <response code="204">Member kicked.</response>
    /// <response code="400">Insufficient permissions or cannot kick higher rank.</response>
    /// <response code="404">Guild or target member not found.</response>
    [ApiDifficulty("intermediate")]
    [ApiPrerequisite("Create or join a guild", "Leader role")]
    [HttpPost("{guildId}/kick")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> KickMember(Guid guildId, [FromBody] KickMemberRequest request)
    {
        try
        {
            var playerId = GetPlayerId();
            await _guildService.KickMemberAsync(guildId, playerId, request.PlayerId);
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

    /// <summary>Promote or demote a guild member.</summary>
    /// <remarks>
    /// Change a member's role. Only the guild leader can promote/demote.
    /// Promoting to Leader transfers guild ownership — you become an Officer.
    /// Valid roles: Member, Officer, Leader.
    /// </remarks>
    /// <param name="guildId">Your guild ID.</param>
    /// <param name="request">Target player and new role.</param>
    /// <response code="200">Member role updated.</response>
    /// <response code="400">Invalid role or insufficient permissions.</response>
    /// <response code="404">Guild or target member not found.</response>
    [ApiDifficulty("intermediate")]
    [ApiGameTip("Promoting someone to Leader transfers ownership — you'll become an Officer. This cannot be undone without the new leader's cooperation.")]
    [ApiPrerequisite("Create a guild", "Leader role")]
    [ApiExample("Promote to Officer", Request = "{\n  \"playerId\": \"d4c3b2a1-8f7e-6d5c-4b3a-2e1f0a9b8c7d\",\n  \"newRole\": \"Officer\"\n}", Response = "{\n  \"message\": \"Player promoted to Officer.\"\n}")]
    [HttpPost("{guildId}/promote")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PromoteMember(Guid guildId, [FromBody] PromoteMemberRequest request)
    {
        try
        {
            var playerId = GetPlayerId();

            if (!Enum.TryParse<GuildRole>(request.NewRole, true, out var role))
                return BadRequest(new { error = "Invalid role. Valid roles: Member, Officer, Leader." });

            await _guildService.PromoteMemberAsync(guildId, playerId, request.PlayerId, role);
            return Ok(new { message = $"Player promoted to {role}." });
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

    /// <summary>Leave your current guild.</summary>
    /// <remarks>
    /// Voluntarily leave the guild you belong to. Guild leaders cannot leave —
    /// transfer leadership first or delete the guild.
    /// </remarks>
    /// <response code="204">You have left the guild.</response>
    /// <response code="400">Not in a guild, or you are the leader.</response>
    [ApiDifficulty("beginner")]
    [ApiGameTip("Leaders must transfer leadership (promote someone to Leader) or delete the guild before they can leave.")]
    [ApiPrerequisite("Join a guild")]
    [HttpPost("leave")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> LeaveGuild()
    {
        try
        {
            var playerId = GetPlayerId();
            await _guildService.LeaveGuildAsync(playerId);
            return NoContent();
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
}
