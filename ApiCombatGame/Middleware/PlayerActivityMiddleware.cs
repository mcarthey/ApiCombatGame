using System.Security.Claims;
using ApiCombatGame.Data;
using Microsoft.EntityFrameworkCore;

namespace ApiCombatGame.Middleware;

public class PlayerActivityMiddleware
{
    private readonly RequestDelegate _next;

    public PlayerActivityMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, GameDbContext dbContext)
    {
        await _next(context);

        // Only track authenticated requests
        var playerIdClaim = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(playerIdClaim) || !Guid.TryParse(playerIdClaim, out var playerId))
            return;

        try
        {
            var today = DateTime.UtcNow.Date;

            var activity = await dbContext.PlayerActivities
                .FirstOrDefaultAsync(a => a.PlayerId == playerId && a.ActivityDate == today);

            if (activity != null)
            {
                activity.RequestCount++;
                activity.LastRequestAt = DateTime.UtcNow;
            }
            else
            {
                dbContext.PlayerActivities.Add(new Models.Domain.PlayerActivity
                {
                    Id = Guid.NewGuid(),
                    PlayerId = playerId,
                    ActivityDate = today,
                    RequestCount = 1,
                    LastRequestAt = DateTime.UtcNow
                });
            }

            await dbContext.SaveChangesAsync();
        }
        catch
        {
            // Activity tracking should never break the request pipeline
        }
    }
}
