using ApiCombatGame.Services.Interfaces;

namespace ApiCombatGame.Services;

public class BackgroundBattleProcessor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _config;
    private readonly ILogger<BackgroundBattleProcessor> _logger;

    public BackgroundBattleProcessor(
        IServiceProvider serviceProvider,
        IConfiguration config,
        ILogger<BackgroundBattleProcessor> logger)
    {
        _serviceProvider = serviceProvider;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Background Battle Processor started.");

        var intervalSeconds = _config.GetValue<int>("GameSettings:BattleProcessingIntervalSeconds", 5);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var battleService = scope.ServiceProvider.GetRequiredService<IBattleService>();

                await battleService.ProcessQueuedBattlesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in background battle processor.");
            }

            await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stoppingToken);
        }

        _logger.LogInformation("Background Battle Processor stopped.");
    }
}
