using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EnvimixDiscordBot.Services;

internal sealed class ClaimExpirationService : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(1);

    private readonly IServiceProvider _provider;
    private readonly TimeProvider _timeProvider;
    private readonly ClaimExpirationOptions _options;
    private readonly ILogger<ClaimExpirationService> _logger;
    private readonly HashSet<int> _pendingStatusUpdates = [];

    public ClaimExpirationService(
        IServiceProvider provider,
        TimeProvider timeProvider,
        ClaimExpirationOptions options,
        ILogger<ClaimExpirationService> logger)
    {
        _provider = provider;
        _timeProvider = timeProvider;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(CheckInterval, _timeProvider);

        do
        {
            try
            {
                await ExpireClaimsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to expire map claims.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task ExpireClaimsAsync(CancellationToken cancellationToken)
    {
        await using var scope = _provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var reporter = scope.ServiceProvider.GetRequiredService<DiscordReporter>();
        var cutoff = _timeProvider.GetUtcNow() - _options.Duration;

        var expiredClaims = db.ConvertedMaps.Where(x =>
            x.ClaimedById != null && x.ClaimedAt != null && x.ClaimedAt <= cutoff && !x.Validated);

        var campaignIds = await expiredClaims
            .Select(x => x.CampaignId)
            .Distinct()
            .ToListAsync(cancellationToken);

        foreach (var campaignId in campaignIds)
        {
            var count = await expiredClaims
                .Where(x => x.CampaignId == campaignId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.ClaimedById, (ulong?)null)
                    .SetProperty(x => x.ClaimedAt, (DateTimeOffset?)null), cancellationToken);

            if (count > 0)
            {
                _logger.LogInformation("Expired {Count} claims in campaign {CampaignId}.", count, campaignId);
                _pendingStatusUpdates.Add(campaignId);
            }
        }

        foreach (var campaignId in _pendingStatusUpdates.ToArray())
        {
            try
            {
                var campaign = await db.Campaigns.FirstAsync(x => x.Id == campaignId, cancellationToken);
                await reporter.UpdateStatusDescriptionAsync(campaign);
                _pendingStatusUpdates.Remove(campaignId);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update status for campaign {CampaignId}; will retry.", campaignId);
            }
        }
    }
}
