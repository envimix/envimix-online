namespace EnvimixWebAPI.Services;

public sealed class RestoreRecordsScheduler(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<RestoreRecordsScheduler> logger) : BackgroundService
{
    private static readonly TimeOnly ScheduledTime = new(8, 0);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = GetDelayUntilNextRun(timeProvider.GetUtcNow());

            try
            {
                await Task.Delay(delay, timeProvider, stoppingToken);

                await using var scope = scopeFactory.CreateAsyncScope();
                await scope.ServiceProvider
                    .GetRequiredService<IEnvimaniaService>()
                    .RestoreRecordsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Scheduled record restoration failed.");
            }
        }
    }

    private static TimeSpan GetDelayUntilNextRun(DateTimeOffset now)
    {
        var nextRun = new DateTimeOffset(
            now.Year,
            now.Month,
            now.Day,
            ScheduledTime.Hour,
            ScheduledTime.Minute,
            0,
            TimeSpan.Zero);

        if (nextRun <= now)
        {
            nextRun = nextRun.AddDays(1);
        }

        return nextRun - now;
    }
}
