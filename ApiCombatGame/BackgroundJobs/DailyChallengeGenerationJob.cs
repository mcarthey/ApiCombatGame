using ApiCombatGame.Data;
using ApiCombatGame.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ApiCombatGame.BackgroundJobs;

public class DailyChallengeGenerationJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DailyChallengeGenerationJob> _logger;

    public DailyChallengeGenerationJob(
        IServiceProvider serviceProvider,
        ILogger<DailyChallengeGenerationJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;
            var nextMidnight = now.Date.AddDays(1);
            var delay = nextMidnight - now;

            _logger.LogInformation("Next challenge generation at {NextMidnight}", nextMidnight);

            await Task.Delay(delay, stoppingToken);

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<GameDbContext>();
                var challengeService = scope.ServiceProvider.GetRequiredService<IChallengeService>();

                // Get all active players (logged in within last 7 days)
                var activePlayers = await context.Players
                    .Where(p => p.LastLoginAt > DateTime.UtcNow.AddDays(-7))
                    .Select(p => p.Id)
                    .ToListAsync(stoppingToken);

                foreach (var playerId in activePlayers)
                {
                    try
                    {
                        await challengeService.GenerateDailyChallenges(playerId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error generating challenges for player {PlayerId}", playerId);
                    }
                }

                _logger.LogInformation("Generated challenges for {Count} players", activePlayers.Count);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating daily challenges");
            }
        }
    }
}
