using EnvimixWebAPI.Entities;
using EnvimixWebAPI.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace EnvimixWebAPI.Endpoints;

public static class RecordEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        group.WithTags("Record");
        group.MapGet("{mapUid}/{car}/{login}/{time:int}", GetRecord);
    }

    private static async Task<Results<Ok<RecordInfo>, NotFound>> GetRecord(
        string mapUid,
        string car,
        string login,
        int time,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var record = await Project(db.Records
            .Where(x =>
                x.MapId == mapUid &&
                x.CarId == car &&
                x.UserId == login &&
                x.Time == time)
            .OrderByDescending(x => x.DrivenAt))
            .FirstOrDefaultAsync(cancellationToken);

        return record is null ? TypedResults.NotFound() : TypedResults.Ok(record);
    }

    internal static Task<RecordInfo[]> GetRecent(
        IQueryable<RecordEntity> records,
        CancellationToken cancellationToken)
        => Project(records
            .OrderByDescending(x => x.DrivenAt)
            .Take(50))
            .ToArrayAsync(cancellationToken);

    internal static IQueryable<RecordInfo> Project(IQueryable<RecordEntity> records)
        => records
            .Select(x => new RecordInfo(
                x.UserId,
                x.User.Nickname,
                x.MapId,
                x.Map.Name,
                x.CarId,
                x.Gravity,
                x.Laps,
                x.Time,
                x.Score,
                x.NbRespawns,
                x.DrivenAt,
                x.SessionId,
                x.Session == null ? null : x.Session.Server.Id,
                x.Removed));
}
