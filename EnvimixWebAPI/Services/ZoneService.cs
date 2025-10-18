using EnvimixWebAPI.Entities;
using ManiaAPI.ManiaPlanetAPI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using System.Collections.Immutable;

namespace EnvimixWebAPI.Services;

public interface IZoneService
{
    Task<IEnumerable<string>> CreateZonesAsync(CancellationToken cancellationToken);
    Task<ImmutableHashSet<string>> GetZonesAsync(CancellationToken cancellationToken);
    Task<bool> IsValidAsync(string zoneName, CancellationToken cancellationToken);
}

public sealed class ZoneService(
    ManiaPlanetAPI mpAPI,
    AppDbContext db,
    HybridCache hybridCache) : IZoneService
{
    public async Task<IEnumerable<string>> CreateZonesAsync(CancellationToken cancellationToken)
    {
        var fetchedZoneNames = (await mpAPI.GetZonesAsync(cancellationToken)).ToList();

        var existingZoneNames = await db.Zones
            .Select(z => z.Name)
            .ToListAsync(cancellationToken);

        if (existingZoneNames.Count == fetchedZoneNames.Count &&
            existingZoneNames.All(fetchedZoneNames.Contains))
        {
            return existingZoneNames;
        }

        var existingSet = existingZoneNames.ToHashSet();
        var fetchedSet = fetchedZoneNames.ToHashSet();

        var toAdd = fetchedSet.Except(existingSet).ToList();
        var toRemove = existingSet.Except(fetchedSet).ToList();

        if (toRemove.Count > 0)
        {
            await db.Zones
                .Where(z => toRemove.Contains(z.Name))
                .ExecuteDeleteAsync(cancellationToken);
        }

        if (toAdd.Count > 0)
        {
            await db.Zones.AddRangeAsync(
                toAdd.Select(name => new ZoneEntity { Name = name }),
                cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
        }

        if (toAdd.Count > 0 || toRemove.Count > 0)
        {
            await hybridCache.RemoveAsync(CacheHelper.GetZonesKey(), CancellationToken.None);
        }

        return fetchedZoneNames;
    }

    public async Task<ImmutableHashSet<string>> GetZonesAsync(CancellationToken cancellationToken)
    {
        return await hybridCache.GetOrCreateAsync(CacheHelper.GetZonesKey(), async token =>
        {
            var zones = await db.Zones
                .Select(x => x.Name)
                .AsNoTracking()
                .ToListAsync(token);
            return zones.ToImmutableHashSet();
        }, new() { Expiration = TimeSpan.FromHours(8) }, cancellationToken: cancellationToken);
    }

    public async Task<bool> IsValidAsync(string zoneName, CancellationToken cancellationToken)
    {
        var zones = await GetZonesAsync(cancellationToken);
        return zones.Contains(zoneName);
    }
}
