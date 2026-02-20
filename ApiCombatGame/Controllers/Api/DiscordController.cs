using System.Security.Claims;
using ApiCombatGame.Filters.Attributes;
using ApiCombatGame.Models.DTOs.Discord;
using ApiCombatGame.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiCombatGame.Controllers.Api;

/// <summary>
/// Discord bot integration — link accounts, register webhooks, and lookup players.
/// </summary>
[ApiController]
[Route("api/v1/discord")]
[Tags("Discord")]
[ApiCategoryMeta("chat", "#5865F2", Order = 27)]
public class DiscordController : ControllerBase
{
    private readonly IDiscordService _discordService;

    public DiscordController(IDiscordService discordService)
    {
        _discordService = discordService;
    }

    /// <summary>Link your Discord account.</summary>
    /// <remarks>
    /// Initiates the Discord linking process. You'll receive a 6-digit verification code.
    /// Use the verify endpoint with this code to complete the link. Once linked,
    /// Discord bots can look up your game profile and send notifications.
    /// </remarks>
    /// <param name="request">Discord user info.</param>
    /// <response code="200">Link initiated. Use the verification code to complete.</response>
    /// <response code="400">Already linked or Discord ID in use.</response>
    [Authorize]
    [ApiDifficulty("beginner")]
    [ApiGameTip("Link your Discord to get battle notifications and use bot commands to check your stats.")]
    [ApiPrerequisite("Register and login")]
    [HttpPost("link")]
    [ProducesResponseType(typeof(DiscordLinkResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DiscordLinkResponse>> LinkAccount([FromBody] LinkDiscordRequest request)
    {
        try
        {
            var playerId = GetPlayerId();
            var result = await _discordService.LinkAccountAsync(playerId, request);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Verify Discord link with code.</summary>
    /// <param name="request">The 6-digit verification code.</param>
    /// <response code="200">Discord account verified and linked.</response>
    /// <response code="400">Invalid code.</response>
    [Authorize]
    [ApiDifficulty("beginner")]
    [ApiPrerequisite("Initiate Discord link")]
    [HttpPost("verify")]
    [ProducesResponseType(typeof(DiscordLinkResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DiscordLinkResponse>> VerifyLink([FromBody] VerifyDiscordRequest request)
    {
        try
        {
            var playerId = GetPlayerId();
            var result = await _discordService.VerifyLinkAsync(playerId, request.VerificationCode);
            return Ok(result);
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

    /// <summary>Get your Discord link status.</summary>
    /// <response code="200">Link status (or null if not linked).</response>
    [Authorize]
    [ApiDifficulty("beginner")]
    [ApiPrerequisite("Register and login")]
    [HttpGet("link")]
    [ProducesResponseType(typeof(DiscordLinkResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<DiscordLinkResponse?>> GetLink()
    {
        var playerId = GetPlayerId();
        var link = await _discordService.GetLinkAsync(playerId);
        return Ok(link);
    }

    /// <summary>Unlink your Discord account.</summary>
    /// <response code="200">Discord account unlinked.</response>
    /// <response code="404">No link found.</response>
    [Authorize]
    [ApiDifficulty("beginner")]
    [HttpDelete("link")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Unlink()
    {
        try
        {
            var playerId = GetPlayerId();
            await _discordService.UnlinkAsync(playerId);
            return Ok(new { message = "Discord account unlinked." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>Register a webhook for game event notifications.</summary>
    /// <remarks>
    /// Register a Discord webhook URL to receive game events (battle results, level-ups, etc.).
    /// Maximum 5 webhooks per player. Specify which event types you want to receive.
    /// </remarks>
    /// <param name="request">Webhook URL and event subscriptions.</param>
    /// <response code="201">Webhook registered.</response>
    /// <response code="400">Invalid URL or limit reached.</response>
    [Authorize]
    [ApiDifficulty("intermediate")]
    [ApiGameTip("Set up a webhook to get real-time battle results posted to your Discord channel.")]
    [ApiPrerequisite("Register and login")]
    [ApiExample("Register battle webhook", Request = "{\n  \"webhookUrl\": \"https://discord.com/api/webhooks/...\",\n  \"label\": \"Battle Alerts\",\n  \"eventTypes\": [\"battle_won\", \"battle_lost\", \"level_up\"]\n}")]
    [HttpPost("webhooks")]
    [ProducesResponseType(typeof(WebhookResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<WebhookResponse>> RegisterWebhook([FromBody] RegisterWebhookRequest request)
    {
        try
        {
            var playerId = GetPlayerId();
            var webhook = await _discordService.RegisterWebhookAsync(playerId, request);
            return Created($"/api/v1/discord/webhooks/{webhook.Id}", webhook);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>List your registered webhooks.</summary>
    /// <response code="200">List of webhooks.</response>
    [Authorize]
    [ApiDifficulty("beginner")]
    [HttpGet("webhooks")]
    [ProducesResponseType(typeof(List<WebhookResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<WebhookResponse>>> GetWebhooks()
    {
        var playerId = GetPlayerId();
        var webhooks = await _discordService.GetWebhooksAsync(playerId);
        return Ok(webhooks);
    }

    /// <summary>Delete a webhook.</summary>
    /// <param name="webhookId">Webhook ID to delete.</param>
    /// <response code="200">Webhook deleted.</response>
    /// <response code="404">Webhook not found.</response>
    [Authorize]
    [ApiDifficulty("beginner")]
    [HttpDelete("webhooks/{webhookId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteWebhook(Guid webhookId)
    {
        try
        {
            var playerId = GetPlayerId();
            await _discordService.DeleteWebhookAsync(playerId, webhookId);
            return Ok(new { message = "Webhook deleted." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>Look up a player by Discord user ID (for bots).</summary>
    /// <remarks>
    /// Discord bots can use this endpoint to look up a player's game profile
    /// by their Discord user ID. Only returns data for verified links.
    /// </remarks>
    /// <param name="discordUserId">Discord user ID.</param>
    /// <response code="200">Player profile.</response>
    /// <response code="404">No verified link found for this Discord ID.</response>
    [ApiDifficulty("beginner")]
    [ApiGameTip("Use this from your Discord bot to display player stats when someone uses a slash command.")]
    [HttpGet("lookup/{discordUserId}")]
    [ProducesResponseType(typeof(DiscordProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DiscordProfileResponse>> LookupByDiscord(string discordUserId)
    {
        if (string.IsNullOrWhiteSpace(discordUserId) || !ulong.TryParse(discordUserId, out _))
            return BadRequest(new { error = "Invalid Discord user ID." });

        var profile = await _discordService.GetProfileByDiscordIdAsync(discordUserId);
        if (profile == null)
            return NotFound(new { error = "No verified account linked to this Discord user." });
        return Ok(profile);
    }

    private Guid GetPlayerId()
    {
        var claim = User.FindFirst("PlayerId") ?? User.FindFirst(ClaimTypes.NameIdentifier);
        if (claim == null || !Guid.TryParse(claim.Value, out var playerId))
            throw new UnauthorizedAccessException("Invalid token.");
        return playerId;
    }
}
