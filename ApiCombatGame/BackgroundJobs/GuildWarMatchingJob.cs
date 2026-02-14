using ApiCombatGame.Services.Interfaces;

namespace ApiCombatGame.BackgroundJobs;

public class GuildWarMatchingJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<GuildWarMatchingJob> _logger;
    private static readonly TimeSpan Interval = TimeSpan.FromHours(6);

    public GuildWarMatchingJob(IServiceProvider serviceProvider, ILogger<GuildWarMatchingJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var warService = scope.ServiceProvider.GetRequiredService<IGuildWarService>();

                // Finalize any expired wars first
                await warService.FinalizeExpiredWarsAsync();

                // Match new guilds for wars
                await warService.MatchGuildsForWarAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in guild war matching job");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }
}
