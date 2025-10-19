using EnvimixWebAPI.Entities;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace EnvimixWebAPI.Endpoints;

public sealed class TotdEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        group.WithTags("Track of the Day");

        group.MapGet("{titleId}", Totd);
    }

    private static async Task<Results<Ok<MapEntity>, ContentHttpResult, NotFound>> Totd(
        string titleId,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        if (titleId == "Nadeo_Envimix@bigbang1112")
        {
            var xml = OldNadeoEnvimixTotd(CreateRandom(titleId));
            return TypedResults.Content(xml, "application/xml");
        }

        var mapCount = await db.Maps
            .Include(m => m.TitlePack)
            .Where(m => m.TitlePack!.Id == titleId)
            .OrderBy(m => m.Id)
            .CountAsync(cancellationToken);

        if (mapCount == 0)
        {
            return TypedResults.NotFound();
        }

        var random = CreateRandom(titleId);
        var randomMapPosition = random.Next(0, mapCount);

        var map = await db.Maps
            .Include(m => m.TitlePack)
            .Where(m => m.TitlePack!.Id == titleId)
            .Skip(randomMapPosition)
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        if (map is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(map);
    }

    private static string OldNadeoEnvimixTotd(Random random)
    {
        var environment = random.Next(0, 4) switch
        {
            0 => "Canyon",
            1 => "Stadium",
            2 => "Valley",
            3 => "Lagoon",
            _ => "Canyon"
        };

        var difficulty = random.Next(0, 5) switch
        {
            0 => "A",
            1 => "B",
            2 => "C",
            3 => "D",
            4 => "E",
            _ => "A"
        };

        var map = difficulty == "E" ? random.Next(1, 6) : random.Next(1, 16);

        var car = random.Next(0, 4) switch
        {
            0 => "CanyonCar",
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
        var seed = HashCode.Combine(DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 86400, titleId);
        return new Random(seed);
    }
}
