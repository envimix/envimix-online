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

    private static async Task<Results<Ok<MapEntity>, NotFound>> Totd(
        string titleId,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var mapCount = await db.Maps
            .Include(m => m.TitlePack)
            .Where(m => m.TitlePack!.Id == titleId)
            .CountAsync(cancellationToken);

        var random = new Random((int)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 86400 * 54) + 4738473);
        var randomMapPosition = random.Next(0, mapCount);

        var map = await db.Maps
            .Include(m => m.TitlePack)
            .Where(m => m.TitlePack!.Id == titleId)
            .Skip(randomMapPosition)
            .FirstOrDefaultAsync(cancellationToken);

        if (map is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(map);
    }
}
