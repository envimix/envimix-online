using EnvimixWebAPI.Entities;
using EnvimixWebAPI.Security;
using Microsoft.EntityFrameworkCore;

namespace EnvimixWebAPI.Services;

/// <summary>
/// Removes IPs from ghosts and replays stored before IP removal was applied on upload.
/// </summary>
public sealed class IpRemovalBackfillService(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<IpRemovalBackfillService> logger) : BackgroundService
{
    private const int BatchSize = 20;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await BackfillAsync(db => db.Ghosts, stoppingToken);
            await BackfillAsync(db => db.Replays, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "IP removal backfill failed.");
        }
    }

    private async Task BackfillAsync<T>(
        Func<AppDbContext, DbSet<T>> set,
        CancellationToken cancellationToken) where T : class, IGbxDataEntity
    {
        var typeName = typeof(T).Name;
        var failedIds = new HashSet<Guid>();
        var processed = 0;

        while (true)
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var batch = await set(db)
                .Where(x => !x.IpsRemoved && !failedIds.Contains(x.Id))
                .OrderBy(x => x.Id)
                .Take(BatchSize)
                .ToListAsync(cancellationToken);

            if (batch.Count == 0)
            {
                break;
            }

            foreach (var entity in batch)
            {
                try
                {
                    var (cleaned, removedCount) = await GbxIpRemover.RemoveIpsAsync(entity.Data, cancellationToken);

                    // Ghosts always carry exactly one IP; replays at least one (session replays can have more)
                    if (entity is GhostEntity ? removedCount != 1 : removedCount == 0)
                    {
                        logger.LogWarning("{Type} {Id} had an unexpected number of IPs removed: {Count}.", typeName, entity.Id, removedCount);
                    }

                    entity.DataBackup ??= entity.Data;
                    entity.Data = cleaned;
                    entity.LastModifiedAt = timeProvider.GetUtcNow();
                    entity.IpsRemoved = true;
                    processed++;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    failedIds.Add(entity.Id);
                    logger.LogWarning(ex, "Failed to remove IPs from {Type} {Id}.", typeName, entity.Id);
                }
            }

            await db.SaveChangesAsync(cancellationToken);
        }

        if (processed > 0 || failedIds.Count > 0)
        {
            logger.LogInformation(
                "IP removal backfill for {Type}: {Processed} processed, {Failed} failed.",
                typeName, processed, failedIds.Count);
        }
    }
}
