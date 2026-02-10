using ApiCombatGame.Services.Interfaces;

namespace ApiCombatGame.BackgroundJobs;

public class WeeklyModifierRotationJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WeeklyModifierRotationJob> _logger;

    public WeeklyModifierRotationJob(
        IServiceProvider serviceProvider,
        ILogger<WeeklyModifierRotationJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;
            var nextMonday = GetNextMonday(now);
            var delay = nextMonday - now;

            _logger.LogInformation("Next modifier rotation scheduled for {NextMonday}", nextMonday);

            await Task.Delay(delay, stoppingToken);

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var modifierService = scope.ServiceProvider.GetRequiredService<IModifierService>();

                await modifierService.RotateModifier();

                _logger.LogInformation("Modifier rotation completed successfully");
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rotating modifier");
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
