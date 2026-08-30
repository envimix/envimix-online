using Microsoft.EntityFrameworkCore;

namespace EnvimixWebAPI.Services;

public sealed class SessionTimeoutBackgroundService(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<SessionTimeoutBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await CloseExpiredSessionsAsync(stoppingToken);

        using var timer = new PeriodicTimer(CheckInterval, timeProvider);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await CloseExpiredSessionsAsync(stoppingToken);
        }
    }

    private async Task CloseExpiredSessionsAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var now = timeProvider.GetUtcNow();

            var closedCount = await db.EnvimaniaSessions
                .Where(x => x.EndedAt == null && x.ExpiresAt <= now)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.EndedAt, x => x.ExpiresAt > x.StartedAt ? x.ExpiresAt : now),
                    cancellationToken);

            if (closedCount > 0)
            {
                logger.LogInformation("Closed {SessionCount} expired Envimania sessions.", closedCount);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Unable to close expired Envimania sessions.");
        }
    }
}
