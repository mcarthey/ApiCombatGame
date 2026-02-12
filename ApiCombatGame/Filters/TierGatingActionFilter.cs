using System.Security.Claims;
using ApiCombatGame.Data;
using ApiCombatGame.Models.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ApiCombatGame.Filters;

/// <summary>
/// Action filter that enforces subscription tier requirements on controller actions
/// marked with [RequiresTier]. Returns 403 if the player's tier is insufficient.
/// </summary>
public class TierGatingActionFilter : IAsyncActionFilter
{
    private readonly GameDbContext _context;

    public TierGatingActionFilter(GameDbContext context)
    {
        _context = context;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var requiresTier = context.ActionDescriptor.EndpointMetadata
            .OfType<RequiresTierAttribute>().FirstOrDefault();

        if (requiresTier != null)
        {
            var playerIdClaim = context.HttpContext.User.FindFirst("PlayerId")
                ?? context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);

            if (playerIdClaim == null || !Guid.TryParse(playerIdClaim.Value, out var playerId))
            {
                context.Result = new UnauthorizedObjectResult(new { error = "Invalid token." });
                return;
            }

            var player = await _context.Players.FindAsync(playerId);
            if (player == null)
            {
                context.Result = new NotFoundObjectResult(new { error = "Player not found." });
                return;
            }

            if (player.CurrentTier < requiresTier.MinimumTier)
            {
                context.Result = new ObjectResult(new
                {
                    error = $"This feature requires {requiresTier.MinimumTier} tier or higher. Current tier: {player.CurrentTier}.",
                    requiredTier = requiresTier.MinimumTier.ToString(),
                    currentTier = player.CurrentTier.ToString(),
                    upgradeUrl = "/Account/Subscription"
                })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
                return;
            }
        }

        await next();
    }
}
