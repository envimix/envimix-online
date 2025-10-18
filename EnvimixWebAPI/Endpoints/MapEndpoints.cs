using EnvimixWebAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace EnvimixWebAPI.Endpoints;

public static class MapEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        group.WithTags("Map");

        group.MapGet("{mapUid}", GetMap);
    }

    private static async Task<IResult> GetMap(
        string mapUid,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var map = await db.Maps
            .Include(x => x.TitlePack)
            .FirstOrDefaultAsync(x => x.Id == mapUid, cancellationToken: cancellationToken);

        if (map is null)
        {
            return TypedResults.NotFound();
        }

        var ungroupedValidations = await db.Records
            .Include(x => x.Map)
            .Where(x => x.Map.Id == mapUid)
            .ToListAsync(cancellationToken);

        var groupedValidations = ungroupedValidations
            .GroupBy(x => new { x.Car, x.Gravity });

        var mapResponse = new MapInfoResponse
        {
            Name = map.Name,
            Uid = map.Id,
            TitlePack = map.TitlePack is null ? null : new()
            {
                Id = map.TitlePack.Id,
                DisplayName = map.TitlePack.DisplayName
            },
            Envimania = []
        };

        return TypedResults.Ok(map);
    }
}
