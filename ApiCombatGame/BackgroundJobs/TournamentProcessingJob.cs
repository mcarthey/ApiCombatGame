using ApiCombatGame.Services.Interfaces;

namespace ApiCombatGame.BackgroundJobs;

public class TournamentProcessingJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TournamentProcessingJob> _logger;
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    public TournamentProcessingJob(IServiceProvider serviceProvider, ILogger<TournamentProcessingJob> logger)
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
                var tournamentService = scope.ServiceProvider.GetRequiredService<ITournamentService>();

                // Create weekly tournament if needed
                await tournamentService.CreateWeeklyTournamentAsync();

                // Process tournament matches
                await tournamentService.ProcessTournamentMatchesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in tournament processing job");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }
}
