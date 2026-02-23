using System.Security.Claims;
using ApiCombatGame.Filters;
using ApiCombatGame.Filters.Attributes;
using ApiCombatGame.Models.DTOs.Guild;
using ApiCombatGame.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiCombatGame.Controllers.Api;

/// <summary>
/// Guild chat — read and post messages in your guild's channel.
/// </summary>
[ApiController]
[Route("api/v1/guild")]
[Authorize]
[Tags("Guild")]
public class GuildChatController : ControllerBase
{
    private readonly IGuildChatService _guildChatService;

    public GuildChatController(IGuildChatService guildChatService)
    {
        _guildChatService = guildChatService;
    }

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

    private Guid GetPlayerId()
    {
        var claim = User.FindFirst("PlayerId") ?? User.FindFirst(ClaimTypes.NameIdentifier);
        if (claim == null || !Guid.TryParse(claim.Value, out var playerId))
            throw new UnauthorizedAccessException("Invalid token.");
        return playerId;
    }
}
