using ApiCombatGame.Models.DTOs.Modifier;
using ApiCombatGame.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ApiCombatGame.Controllers.Api;

/// <summary>
/// Environmental modifiers that change battle rules weekly.
/// </summary>
[ApiController]
[Route("api/v1/modifiers")]
public class ModifierController : ControllerBase
{
    private readonly IModifierService _modifierService;

    public ModifierController(IModifierService modifierService)
    {
        _modifierService = modifierService;
    }

    /// <summary>
    /// Get the current environmental modifier affecting all battles.
    /// </summary>
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

    /// <summary>
    /// Preview next week's modifier.
    /// </summary>
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
