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
        var zoneNames = await mpAPI.GetZonesAsync(cancellationToken);

        // Clear zones
        db.Zones.RemoveRange(db.Zones);

        await db.Zones.AddRangeAsync(zoneNames.Select(zoneName => new ZoneEntity
        {
            Name = zoneName
        }), cancellationToken);

        await db.SaveChangesAsync(cancellationToken);

        await hybridCache.RemoveAsync(CacheHelper.GetZonesKey(), CancellationToken.None);

        return zoneNames;
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
