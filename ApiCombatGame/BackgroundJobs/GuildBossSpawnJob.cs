using ApiCombatGame.Data;
using ApiCombatGame.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ApiCombatGame.BackgroundJobs;

public class GuildBossSpawnJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<GuildBossSpawnJob> _logger;

    public GuildBossSpawnJob(
        IServiceProvider serviceProvider,
        ILogger<GuildBossSpawnJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // Run once per week on Monday at 00:00 UTC
            var now = DateTime.UtcNow;
            var nextMonday = GetNextMonday(now);
            var delay = nextMonday - now;

            _logger.LogInformation("Next boss spawn scheduled for {NextMonday}", nextMonday);

            await Task.Delay(delay, stoppingToken);

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<GameDbContext>();
                var bossService = scope.ServiceProvider.GetRequiredService<IGuildBossService>();

                var guildIds = await context.Guilds
                    .Select(g => g.Id)
                    .ToListAsync(stoppingToken);

                foreach (var guildId in guildIds)
                {
                    try
                    {
                        await bossService.SpawnBossForGuild(guildId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error spawning boss for guild {GuildId}", guildId);
                    }
                }

                _logger.LogInformation("Spawned bosses for {Count} guilds", guildIds.Count);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error spawning guild bosses");
            }
        }
    }

    private static DateTime GetNextMonday(DateTime from)
    {
        var daysUntilMonday = ((int)DayOfWeek.Monday - (int)from.DayOfWeek + 7) % 7;
        if (daysUntilMonday == 0) daysUntilMonday = 7;

        var nextMonday = from.Date.AddDays(daysUntilMonday);
        return new DateTime(nextMonday.Year, nextMonday.Month, nextMonday.Day, 0, 0, 0, DateTimeKind.Utc);
    }
}
