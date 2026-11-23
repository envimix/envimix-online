using EnvimixWebAPI.Entities;
using EnvimixWebAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace EnvimixWebAPI.Services;

public interface IMapService
{
    Task<MapEntity?> GetAsync(string mapUid, CancellationToken cancellationToken = default);
    Task<MapEntity> GetAddOrUpdateAsync(string mapUid, string titleId, CancellationToken cancellationToken = default);
    Task<MapEntity> GetAddOrUpdateAsync(MapInfo mapInfo, ServerEntity? server, CancellationToken cancellationToken = default);
    Task<MapEntity?> GetWithDownloadAsync(string mapUid, CancellationToken cancellationToken);
    Task<int> GetCampaignMedalPointsAsync(int campaignId, string login, CancellationToken cancellationToken);
}

public sealed class MapService(AppDbContext db) : IMapService
{
    /// <summary>
    /// Medal points the player earned across a campaign, mirroring the in-game world map progression:
    /// best default car time per map (normal gravity, full race), 5 for author, 3 gold, 2 silver, 1 bronze.
    /// </summary>
    public async Task<int> GetCampaignMedalPointsAsync(int campaignId, string login, CancellationToken cancellationToken)
    {
        var bestTimes = await db.Records
            .Where(x => x.UserId == login
                && !x.Removed
                && x.Time > 0
                && x.Gravity == 0
                && x.Map.CampaignId == campaignId
                && x.Map.IsCampaignMap
                && (x.Map.Laps <= 0 || x.Laps == x.Map.Laps)
                && (x.Map.DefaultCarId != null && x.Map.DefaultCarId != ""
                    ? x.CarId == x.Map.DefaultCarId
                    : x.CarId == x.Map.Collection + "Car"))
            .GroupBy(x => new { x.MapId, x.Map.AuthorTime, x.Map.GoldTime, x.Map.SilverTime, x.Map.BronzeTime })
            .Select(g => new
            {
                g.Key.AuthorTime,
                g.Key.GoldTime,
                g.Key.SilverTime,
                g.Key.BronzeTime,
                Time = g.Min(x => x.Time)
            })
            .ToListAsync(cancellationToken);

        return bestTimes.Sum(x =>
            x.Time <= x.AuthorTime ? 5 :
            x.Time <= x.GoldTime ? 3 :
            x.Time <= x.SilverTime ? 2 :
            x.Time <= x.BronzeTime ? 1 : 0);
    }

    public async Task<MapEntity?> GetAsync(string mapUid, CancellationToken cancellationToken = default)
    {
        return await db.Maps
            .Include(x => x.TitlePack)
            .Include(x => x.Campaign)
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

        if (map?.IsCampaignMap == true)
        {
            return map;
        }

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

        if (mapInfo.Laps > 0)
        {
            map.Laps = mapInfo.Laps;
        }

        if (!string.IsNullOrWhiteSpace(mapInfo.Collection))
        {
            map.Collection = mapInfo.Collection;
        }

        if (!string.IsNullOrWhiteSpace(mapInfo.AuthorLogin))
        {
            map.AuthorLogin = mapInfo.AuthorLogin;
        }

        if (!string.IsNullOrWhiteSpace(mapInfo.AuthorNickname))
        {
            map.AuthorNickname = mapInfo.AuthorNickname;
        }

        map.AuthorTime = mapInfo.AuthorTime;
        map.GoldTime = mapInfo.GoldTime;
        map.SilverTime = mapInfo.SilverTime;
        map.BronzeTime = mapInfo.BronzeTime;
        if (!string.IsNullOrWhiteSpace(mapInfo.DefaultCar))
        {
            map.DefaultCarId = mapInfo.DefaultCar;
        }

        await db.SaveChangesAsync(cancellationToken);

        return map;
    }

    public async Task<MapEntity?> GetWithDownloadAsync(string mapUid, CancellationToken cancellationToken)
    {
        return await db.Maps
            .Include(x => x.Data)
            .FirstOrDefaultAsync(x => x.Id == mapUid, cancellationToken);
    }
}
