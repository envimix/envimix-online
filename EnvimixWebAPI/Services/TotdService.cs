using EnvimixWebAPI.Models;
using Microsoft.EntityFrameworkCore;
using System.IO.Hashing;
using System.Text;

namespace EnvimixWebAPI.Services;

public interface ITotdService
{
    Task<MapInfo?> GetMapAsync(string titleId, CancellationToken cancellationToken);
}

public sealed class TotdService(AppDbContext db) : ITotdService
{
    public async Task<MapInfo?> GetMapAsync(string titleId, CancellationToken cancellationToken)
    {
        var mapCount = await db.Maps
            .Include(m => m.TitlePack)
            .Where(m => m.TitlePack!.Id == titleId && m.IsCampaignMap)
            .CountAsync(cancellationToken);

        if (mapCount == 0)
        {
            return null;
        }

        var random = CreateRandom(titleId);
        var randomMapPosition = random.Next(0, mapCount);

        var map = await db.Maps
            .Where(m => m.TitlePackId == titleId && m.IsCampaignMap)
            .OrderBy(m => m.Order)
            .Skip(randomMapPosition)
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        if (map is null)
        {
            return null;
        }

        return new MapInfo
        {
            Name = map.Name,
            Collection = map.Collection,
            Uid = map.Id,
            Order = map.Order
        };
    }

    public static string GetOldMapXml(string titleId)
    {
        var random = CreateRandom(titleId);

        var environment = random.Next(0, 4) switch
        {
            1 => "Stadium",
            2 => "Valley",
            3 => "Lagoon",
            _ => "Canyon"
        };

        var difficulty = random.Next(0, 5) switch
        {
            1 => "B",
            2 => "C",
            3 => "D",
            4 => "E",
            _ => "A"
        };

        var map = difficulty == "E" ? random.Next(1, 6) : random.Next(1, 16);

        var car = random.Next(0, 4) switch
        {
            1 => "StadiumCar",
            2 => "ValleyCar",
            3 => "LagoonCar",
            _ => "CanyonCar"
        };

        return $"""
<TRACK_DAY>
    <Environment>{environment}</Environment>
    <Difficulty>{difficulty}</Difficulty>
    <Map>{map}</Map>
    <Car>{car}</Car>
    <Multiplier>1.0</Multiplier>
</TRACK_DAY>
""";
    }

    private static Random CreateRandom(string titleId)
    {
        var data = Encoding.UTF8.GetBytes(titleId)
            .Concat(BitConverter.GetBytes(DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 86400 * 1112))
            .ToArray();
        return new Random((int)Crc32.HashToUInt32(data));
    }
}
