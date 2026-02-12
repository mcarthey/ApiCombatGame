using ApiCombatGame.Data;
using ApiCombatGame.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace ApiCombatGame.BackgroundJobs;

public class GuildInviteExpiryJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<GuildInviteExpiryJob> _logger;

    public GuildInviteExpiryJob(IServiceScopeFactory scopeFactory, ILogger<GuildInviteExpiryJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<GameDbContext>();

                var expiredInvites = await context.GuildInvites
                    .Where(i => i.Status == GuildInviteStatus.Pending && i.ExpiresAt <= DateTime.UtcNow)
                    .ToListAsync(stoppingToken);

                if (expiredInvites.Count > 0)
                {
                    foreach (var invite in expiredInvites)
                        invite.Status = GuildInviteStatus.Expired;

                    await context.SaveChangesAsync(stoppingToken);
                    _logger.LogInformation("Expired {Count} guild invites", expiredInvites.Count);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error expiring guild invites");
            }

            // Run once daily
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }
}
