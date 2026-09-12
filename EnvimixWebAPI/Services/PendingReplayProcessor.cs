using EnvimixWebAPI.Models;
using System.Threading.Channels;

namespace EnvimixWebAPI.Services;

public sealed class PendingReplayProcessor(
    Channel<PendingReplaySubmission> pendingReplayChannel,
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<PendingReplayProcessor> logger) : BackgroundService
{
    private const int MaxPendingCount = 100;
    private static readonly TimeSpan RetryInterval = TimeSpan.FromSeconds(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var pending = new List<PendingReplaySubmission>();
        using var timer = new PeriodicTimer(RetryInterval, timeProvider);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            while (pending.Count < MaxPendingCount && pendingReplayChannel.Reader.TryRead(out var submission))
            {
                pending.Add(submission);
                logger.LogInformation(
                    "Enqueued pending {ReplayKind} replay from server {ServerLogin} for player {PlayerLogin} on map {MapUid}; it expires at {ExpiresAt}.",
                    submission.IsValidation ? "validation" : "main",
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
                        "Discarded replay for {PlayerLogin} on {MapUid}; its record did not appear within one minute.",
                        submission.PlayerLogin,
                        submission.MapUid);
                    pending.RemoveAt(index);
                    continue;
                }

                try
                {
                    await using var scope = scopeFactory.CreateAsyncScope();
                    var service = scope.ServiceProvider.GetRequiredService<IReplaySubmissionService>();
                    var result = await service.TryAttachAsync(submission, stoppingToken);
                    if (result != ReplayAttachmentResult.RecordNotFound)
                    {
                        pending.RemoveAt(index);
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogError(ex, "Unable to process pending replay for {PlayerLogin} on {MapUid}.", submission.PlayerLogin, submission.MapUid);
                }
            }
        }
    }
}
