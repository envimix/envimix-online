using EnvimixWebAPI.Models;
using System.Threading.Channels;

namespace EnvimixWebAPI.Services;

public sealed class PendingGhostProcessor(
    Channel<PendingGhostSubmission> pendingGhostChannel,
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<PendingGhostProcessor> logger) : BackgroundService
{
    private const int MaxPendingCount = 100;
    private static readonly TimeSpan RetryInterval = TimeSpan.FromSeconds(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var pending = new List<PendingGhostSubmission>();
        using var timer = new PeriodicTimer(RetryInterval, timeProvider);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            while (pending.Count < MaxPendingCount && pendingGhostChannel.Reader.TryRead(out var submission))
            {
                pending.Add(submission);
                logger.LogInformation(
                    "Enqueued pending ghost from server {ServerLogin} for player {PlayerLogin} on map {MapUid}; it expires at {ExpiresAt}.",
                    submission.ServerLogin,
                    submission.PlayerLogin,
                    submission.MapUid,
                    submission.ExpiresAt);
            }

            for (var index = pending.Count - 1; index >= 0; index--)
            {
                var submission = pending[index];
                if (submission.ExpiresAt <= timeProvider.GetUtcNow())
                {
                    logger.LogWarning(
                        "Discarded ghost for {PlayerLogin} on {MapUid}; its record did not appear within one minute.",
                        submission.PlayerLogin,
                        submission.MapUid);
                    pending.RemoveAt(index);
                    continue;
                }

                try
                {
                    await using var scope = scopeFactory.CreateAsyncScope();
                    var service = scope.ServiceProvider.GetRequiredService<IGhostSubmissionService>();
                    var result = await service.TryAttachAsync(submission, stoppingToken);
                    if (result != GhostAttachmentResult.RecordNotFound)
                    {
                        pending.RemoveAt(index);
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogError(ex, "Unable to process pending ghost for {PlayerLogin} on {MapUid}.", submission.PlayerLogin, submission.MapUid);
                }
            }
        }
    }
}
