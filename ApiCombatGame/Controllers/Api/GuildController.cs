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
    private readonly IGuildTreasuryService _guildTreasuryService;
    private readonly IGuildChatService _guildChatService;
    private readonly IGuildStrategyService _guildStrategyService;
    private readonly ILogger<GuildController> _logger;

    public GuildController(
        IGuildService guildService,
        IGuildTreasuryService guildTreasuryService,
        IGuildChatService guildChatService,
        IGuildStrategyService guildStrategyService,
        ILogger<GuildController> logger)
    {
        _guildService = guildService;
        _guildTreasuryService = guildTreasuryService;
        _guildChatService = guildChatService;
        _guildStrategyService = guildStrategyService;
        _logger = logger;
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

    // ── Treasury Endpoints ──

    /// <summary>View guild treasury and available upgrades.</summary>
    /// <remarks>
    /// Shows the guild's gold balance, current upgrade levels, and what upgrades can be purchased.
    /// </remarks>
    /// <param name="guildId">The guild to view treasury of.</param>
    /// <response code="200">Treasury balance with upgrade options.</response>
    /// <response code="404">Guild not found.</response>
    [ApiDifficulty("intermediate")]
    [ApiGameTip("Coordinate deposits with your guildmates to save up for powerful upgrades like the 20% gold bonus.")]
    [ApiPrerequisite("Join a guild")]
    [HttpGet("{guildId}/treasury")]
    [ProducesResponseType(typeof(TreasuryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTreasury(Guid guildId)
    {
        try
        {
            var (guild, upgrades) = await _guildTreasuryService.GetTreasuryAsync(guildId);
            return Ok(new TreasuryResponse
            {
                Balance = guild.TreasuryBalance,
                GoldBonusPercent = guild.GoldBonusPercent,
                MaxRaidAttempts = guild.MaxRaidAttempts,
                MaxMembers = guild.MaxMembers,
                AvailableUpgrades = upgrades.Select(u => new GuildUpgradeOption
                {
                    Id = u.Id,
                    Name = u.Name,
                    Description = u.Description,
                    Cost = u.Cost,
                    CanAfford = u.CanAfford,
                    AlreadyPurchased = u.AlreadyPurchased
                }).ToList()
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>Purchase a guild upgrade from the treasury.</summary>
    /// <remarks>
    /// Spend treasury gold on permanent guild upgrades. Only the guild leader can make purchases.
    /// Available upgrades: max_members_30, max_members_50, gold_bonus_10, gold_bonus_20,
    /// raid_attempts_4, raid_attempts_5.
    /// </remarks>
    /// <param name="guildId">Your guild ID.</param>
    /// <param name="request">The upgrade to purchase.</param>
    /// <response code="200">Upgrade purchased. New treasury state returned.</response>
    /// <response code="400">Insufficient funds, already purchased, or not the leader.</response>
    /// <response code="404">Guild or upgrade not found.</response>
    [ApiDifficulty("intermediate")]
    [ApiGameTip("The gold_bonus_10 upgrade (30,000g) pays for itself over time as every member earns 10% more gold.")]
    [ApiPrerequisite("Create a guild", "Deposit gold into treasury")]
    [ApiExample("Purchase an upgrade", Request = "{\n  \"upgradeId\": \"gold_bonus_10\"\n}", Response = "{\n  \"balance\": 20000,\n  \"goldBonusPercent\": 10,\n  \"maxRaidAttempts\": 3,\n  \"maxMembers\": 20\n}")]
    [HttpPost("{guildId}/treasury/spend")]
    [ProducesResponseType(typeof(TreasuryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PurchaseUpgrade(Guid guildId, [FromBody] TreasurySpendRequest request)
    {
        try
        {
            var playerId = GetPlayerId();
            var guild = await _guildTreasuryService.PurchaseUpgradeAsync(guildId, playerId, request.UpgradeId);

            return Ok(new
            {
                balance = guild.TreasuryBalance,
                goldBonusPercent = guild.GoldBonusPercent,
                maxRaidAttempts = guild.MaxRaidAttempts,
                maxMembers = guild.MaxMembers
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

    /// <summary>Deposit personal gold into the guild treasury.</summary>
    /// <remarks>
    /// Transfer gold from your personal balance to the guild treasury. This increases your
    /// contribution points. Any guild member can deposit.
    /// </remarks>
    /// <param name="guildId">Your guild ID.</param>
    /// <param name="request">Amount of gold to deposit.</param>
    /// <response code="200">Deposit successful. New balances returned.</response>
    /// <response code="400">Insufficient personal gold or not a member.</response>
    /// <response code="404">Guild not found.</response>
    [ApiDifficulty("beginner")]
    [ApiGameTip("Depositing gold increases your contribution points — guilds often use this to track member participation.")]
    [ApiPrerequisite("Join a guild", "Have gold to deposit")]
    [HttpPost("{guildId}/treasury/deposit")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DepositToTreasury(Guid guildId, [FromBody] TreasuryDepositRequest request)
    {
        try
        {
            var playerId = GetPlayerId();
            var guild = await _guildTreasuryService.DepositAsync(guildId, playerId, request.Amount);

            return Ok(new
            {
                treasuryBalance = guild.TreasuryBalance,
                message = $"Deposited {request.Amount:N0} gold to the guild treasury."
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

    // ── Chat Endpoints ──

    /// <summary>Get guild chat messages.</summary>
    /// <remarks>
    /// Returns recent chat messages in chronological order. Supports cursor-based pagination
    /// using the `before` parameter to load older messages.
    /// </remarks>
    /// <param name="guildId">The guild chat to read.</param>
    /// <param name="limit">Maximum messages to return (default 50, max 100).</param>
    /// <param name="before">Message ID cursor — returns messages older than this one.</param>
    /// <response code="200">Array of chat messages.</response>
    [ApiDifficulty("beginner")]
    [ApiGameTip("System messages (join/leave/boss events) appear automatically in the chat feed alongside player messages.")]
    [ApiPrerequisite("Join a guild")]
    [HttpGet("{guildId}/chat")]
    [ProducesResponseType(typeof(List<ChatMessageResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetChat(Guid guildId, [FromQuery] int limit = 50, [FromQuery] Guid? before = null)
    {
        var messages = await _guildChatService.GetMessagesAsync(guildId, limit, before);
        return Ok(messages.Select(m => new ChatMessageResponse
        {
            MessageId = m.Id,
            PlayerId = m.PlayerId,
            Username = m.Player?.Username,
            Message = m.Message,
            MessageType = m.MessageType,
            CreatedAt = m.CreatedAt
        }));
    }

    /// <summary>Post a message to guild chat.</summary>
    /// <remarks>
    /// Send a text message to your guild's chat channel. Messages are limited to 500 characters.
    /// You must be a member of the guild.
    /// </remarks>
    /// <param name="guildId">Your guild ID.</param>
    /// <param name="request">The message text.</param>
    /// <response code="201">Message posted.</response>
    /// <response code="400">Empty message, too long, or not a guild member.</response>
    [ApiDifficulty("beginner")]
    [ApiPrerequisite("Join a guild")]
    [HttpPost("{guildId}/chat")]
    [ProducesResponseType(typeof(ChatMessageResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PostChat(Guid guildId, [FromBody] PostChatRequest request)
    {
        try
        {
            var playerId = GetPlayerId();
            var message = await _guildChatService.PostMessageAsync(guildId, playerId, request.Message);
            var response = new ChatMessageResponse
            {
                MessageId = message.Id,
                PlayerId = message.PlayerId,
                Username = message.Player?.Username,
                Message = message.Message,
                MessageType = message.MessageType,
                CreatedAt = message.CreatedAt
            };
            return Created($"/api/v1/guild/{guildId}/chat", response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // ── Strategy Library Endpoints ──

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
