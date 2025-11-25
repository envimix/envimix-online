using EnvimixWebAPI.Entities;
using EnvimixWebAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace EnvimixWebAPI.Services;

public interface IMapService
{
    Task<MapEntity?> GetAsync(string mapUid, CancellationToken cancellationToken = default);
    Task<MapEntity> GetAddOrUpdateAsync(string mapUid, string titleId, CancellationToken cancellationToken = default);
    Task<MapEntity> GetAddOrUpdateAsync(MapInfo mapInfo, ServerEntity? server, CancellationToken cancellationToken = default);
}

public sealed class MapService(AppDbContext db) : IMapService
{
    public async Task<MapEntity?> GetAsync(string mapUid, CancellationToken cancellationToken = default)
    {
        return await db.Maps
            .Include(x => x.TitlePack)
            .FirstOrDefaultAsync(x => x.Id == mapUid, cancellationToken);
    }

    public async Task<MapEntity> GetAddOrUpdateAsync(string mapUid, string titleId, CancellationToken cancellationToken = default)
    {
        var map = await GetAsync(mapUid, cancellationToken);

        if (map is null)
        {
            map = new MapEntity
            {
                Id = mapUid,
                TitlePackId = titleId
            };

            await db.Maps.AddAsync(map, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
        }

        return map;
    }

    public async Task<MapEntity> GetAddOrUpdateAsync(MapInfo mapInfo, ServerEntity? server, CancellationToken cancellationToken = default)
    {
        var map = await GetAsync(mapInfo.Uid, cancellationToken);

        if (map is null)
        {
            map = new MapEntity
            {
                Id = mapInfo.Uid,
                //TitlePackId = mapInfo
                FirstAppearedOnServer = server
            };

            await db.Maps.AddAsync(map, cancellationToken);
        }

        map.Name = mapInfo.Name;

        if (!string.IsNullOrWhiteSpace(mapInfo.Collection))
        {
            map.Collection = mapInfo.Collection;
        }

        await db.SaveChangesAsync(cancellationToken);

        return map;
    }
}
