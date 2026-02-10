using ApiCombatGame.Services.Interfaces;

namespace ApiCombatGame.BackgroundJobs;

public class StrategyDecayJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<StrategyDecayJob> _logger;

    public StrategyDecayJob(
        IServiceProvider serviceProvider,
        ILogger<StrategyDecayJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // Run once per day at 2 AM UTC
            var now = DateTime.UtcNow;
            var next2AM = now.Date.AddHours(2);
            if (now.Hour >= 2) next2AM = next2AM.AddDays(1);

            var delay = next2AM - now;

            _logger.LogInformation("Next strategy decay at {Next2AM}", next2AM);

            await Task.Delay(delay, stoppingToken);

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var marketplace = scope.ServiceProvider.GetRequiredService<IStrategyMarketplaceService>();

                await marketplace.ApplyStrategyDecay();

                _logger.LogInformation("Strategy decay applied successfully");
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying strategy decay");
            }
        }
    }
}
