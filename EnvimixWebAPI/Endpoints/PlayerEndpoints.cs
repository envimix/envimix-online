using EnvimixWebAPI.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace EnvimixWebAPI.Endpoints;

public static class PlayerEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        group.WithTags("Player");
        group.MapGet("{login}", GetPlayer);
    }

    private static async Task<Results<Ok<PlayerInfo>, NotFound>> GetPlayer(
        string login,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var player = await db.Users
            .Where(x => x.Id == login)
            .Select(x => new
            {
                Login = x.Id,
                x.Nickname,
                Zone = x.Zone == null ? null : x.Zone.Name,
                RecordCount = x.Records.Count
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (player is null)
        {
            return TypedResults.NotFound();
        }

        var records = await RecordEndpoints.GetRecent(
            db.Records.Where(x => x.UserId == login), cancellationToken);
        return TypedResults.Ok(new PlayerInfo(
            player.Login,
            player.Nickname,
            player.Zone,
            player.RecordCount,
            records));
    }
}
