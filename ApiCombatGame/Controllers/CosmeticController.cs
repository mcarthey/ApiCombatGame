using System.Security.Claims;
using ApiCombatGame.Filters.Attributes;
using ApiCombatGame.Models.DTOs.Cosmetic;
using ApiCombatGame.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiCombatGame.Controllers;

/// <summary>
/// Cosmetic shop and premium currency (gems). Customize your profile and units.
/// </summary>
[ApiCategoryMeta("sparkles", "#a855f7", Order = 22)]
[ApiController]
[Route("api/v1/cosmetics")]
[Authorize]
[Tags("Cosmetics")]
public class CosmeticController : ControllerBase
{
    private readonly ICosmeticService _cosmeticService;

    public CosmeticController(ICosmeticService cosmeticService)
    {
        _cosmeticService = cosmeticService;
    }

    /// <summary>Browse the cosmetic shop.</summary>
    /// <remarks>
    /// Shows all available cosmetics with prices in gems and gold. Includes your current
    /// balance and whether you already own each item.
    ///
    /// Categories: unit_skin, profile_border, card_back, battle_effect.
    /// Rarities: common, rare, epic, legendary (legendary items are gems-only).
    /// Gems are earned from tournaments, battle pass milestones, and special events.
    /// </remarks>
    /// <response code="200">Shop listing with your balance and ownership status.</response>
    [ApiDifficulty("beginner")]
    [ApiGameTip("Legendary items can only be purchased with gems — save your premium currency for these exclusive cosmetics.")]
    [ApiPrerequisite("Register and login")]
    [HttpGet("shop")]
    [ProducesResponseType(typeof(CosmeticShopResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetShop()
    {
        var playerId = GetPlayerId();
        var result = await _cosmeticService.GetShopAsync(playerId);
        return Ok(result);
    }

    /// <summary>View your owned cosmetics.</summary>
    /// <response code="200">List of cosmetics you own with equipped status.</response>
    [ApiDifficulty("beginner")]
    [ApiPrerequisite("Register and login")]
    [HttpGet("owned")]
    [ProducesResponseType(typeof(List<PlayerCosmeticResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOwnedCosmetics()
    {
        var playerId = GetPlayerId();
        var result = await _cosmeticService.GetOwnedCosmeticsAsync(playerId);
        return Ok(result);
    }

    /// <summary>Purchase a cosmetic item.</summary>
    /// <remarks>
    /// Buy a cosmetic from the shop using gems or gold. Each item shows available
    /// payment options — legendary items are gems-only (goldPrice = 0).
    /// </remarks>
    /// <param name="cosmeticId">The cosmetic item to purchase.</param>
    /// <param name="request">Payment method: "gems" or "gold".</param>
    /// <response code="200">Purchase successful.</response>
    /// <response code="400">Insufficient funds or already owned.</response>
    [ApiDifficulty("beginner")]
    [ApiGameTip("Gold prices are much higher than gem equivalents — gems are the better deal if you have them.")]
    [ApiPrerequisite("Have enough gems or gold")]
    [HttpPost("purchase/{cosmeticId}")]
    [ProducesResponseType(typeof(PlayerCosmeticResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Purchase(Guid cosmeticId, [FromBody] PurchaseCosmeticRequest request)
    {
        try
        {
            var playerId = GetPlayerId();
            var result = await _cosmeticService.PurchaseCosmeticAsync(playerId, cosmeticId, request.PaymentMethod);
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

    /// <summary>Equip a cosmetic item.</summary>
    /// <remarks>
    /// Set a cosmetic as active. Only one cosmetic per category can be equipped at a time.
    /// Equipping a new profile_border unequips the previous one automatically.
    /// </remarks>
    /// <param name="cosmeticId">The cosmetic to equip.</param>
    /// <response code="200">Cosmetic equipped.</response>
    /// <response code="400">You don't own this cosmetic.</response>
    [ApiDifficulty("beginner")]
    [ApiPrerequisite("Own the cosmetic")]
    [HttpPost("equip/{cosmeticId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Equip(Guid cosmeticId)
    {
        try
        {
            var playerId = GetPlayerId();
            await _cosmeticService.EquipCosmeticAsync(playerId, cosmeticId);
            return Ok(new { message = "Cosmetic equipped successfully." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Check your gem and gold balance.</summary>
    /// <response code="200">Your current balance and lifetime gem stats.</response>
    [ApiDifficulty("beginner")]
    [ApiPrerequisite("Register and login")]
    [HttpGet("balance")]
    [ProducesResponseType(typeof(GemBalanceResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBalance()
    {
        var playerId = GetPlayerId();
        var result = await _cosmeticService.GetGemBalanceAsync(playerId);
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
