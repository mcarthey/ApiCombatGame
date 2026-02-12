using ApiCombatGame.Filters.Attributes;
using ApiCombatGame.Models.DTOs.Modifier;
using ApiCombatGame.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ApiCombatGame.Controllers.Api;

/// <summary>
/// Environmental modifiers that change battle rules weekly.
/// </summary>
[ApiController]
[Tags("Modifiers")]
[Route("api/v1/modifiers")]
[ApiCategoryMeta("bolt", "#eab308", Order = 10)]
public class ModifierController : ControllerBase
{
    private readonly IModifierService _modifierService;

    public ModifierController(IModifierService modifierService)
    {
        _modifierService = modifierService;
    }

    /// <summary>Get the active environmental modifier.</summary>
    /// <remarks>
    /// Environmental modifiers rotate weekly and change the rules of battle for everyone.
    /// No authentication required. Check this before every battle to adapt your strategy.
    /// Returns a "Normal" placeholder when no modifier is active.
    /// </remarks>
    /// <response code="200">The current modifier with name, description, and active dates.</response>
    [ApiDifficulty("beginner")]
    [ApiGameTip("Adapt your team composition to the current modifier for a significant competitive edge.")]
    [ApiExample("Get active modifier", Response = "{\n  \"modifierId\": \"m1a2b3c4-d5e6-7890-abcd-ef1234567890\",\n  \"name\": \"Arcane Surge\",\n  \"description\": \"Magic damage increased by 25% for all units\",\n  \"startDate\": \"2026-02-10T00:00:00Z\",\n  \"endDate\": \"2026-02-17T00:00:00Z\"\n}")]
    [HttpGet("current")]
    [ProducesResponseType(typeof(ModifierResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ModifierResponse>> GetCurrentModifier()
    {
        var modifier = await _modifierService.GetCurrentModifier();
        if (modifier == null)
        {
            return Ok(new ModifierResponse
            {
                Name = "Normal",
                Description = "No environmental effects this week"
            });
        }

        return Ok(new ModifierResponse
        {
            ModifierId = modifier.Id,
            Name = modifier.Name,
            Description = modifier.Description,
            StartDate = modifier.StartDate,
            EndDate = modifier.EndDate
        });
    }

    /// <summary>Preview next week's modifier.</summary>
    /// <remarks>
    /// See what environmental effect is coming next. Use this intel to prepare your team
    /// and strategy ahead of the rotation. Not always available.
    /// </remarks>
    /// <response code="200">Upcoming modifier details.</response>
    /// <response code="404">No upcoming modifier has been scheduled yet.</response>
    [ApiDifficulty("beginner")]
    [ApiGameTip("Prepare your team for the next rotation by checking upcoming modifiers in advance.")]
    [HttpGet("upcoming")]
    [ProducesResponseType(typeof(ModifierResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ModifierResponse>> GetUpcomingModifier()
    {
        var modifier = await _modifierService.GetUpcomingModifier();
        if (modifier == null)
        {
            return NotFound(new { error = "No upcoming modifier scheduled" });
        }

        return Ok(new ModifierResponse
        {
            ModifierId = modifier.Id,
            Name = modifier.Name,
            Description = modifier.Description,
            StartDate = modifier.StartDate,
            EndDate = modifier.EndDate
        });
    }
}
