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
        var record = await ProjectWithId(db.Records
            .Where(x =>
                x.MapId == mapUid &&
                x.CarId == car &&
                x.UserId == login &&
                x.Time == time)
            .OrderByDescending(x => x.DrivenAt))
            .FirstOrDefaultAsync(cancellationToken);

        if (record is null)
        {
            return TypedResults.NotFound();
        }

        var checkpoints = await db.Checkpoints
            .Where(x => x.Record.Id == record.Id)
            .OrderBy(x => x.Time)
            .ThenBy(x => x.Id)
            .Select(x => new RecordCheckpointInfo(
                x.Time,
                x.Score,
                x.NbRespawns,
                x.Distance,
                x.Speed))
            .ToArrayAsync(cancellationToken);

        return TypedResults.Ok(record.ToRecordInfo(null) with { Checkpoints = checkpoints });
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
                x.Map.Laps,
                x.CarId,
                x.Gravity,
                x.Laps,
                x.Time,
                x.Score,
                x.NbRespawns,
                x.DrivenAt,
                x.SessionId,
                x.Session == null ? null : x.Session.Server.Id,
                x.Session == null ? null : x.Session.Server.Name,
                x.TitleId,
                x.Title == null ? null : x.Title.DisplayName,
                x.GhostId,
                null,
                x.Removed));

    internal static IQueryable<RecordProjection> ProjectWithId(IQueryable<RecordEntity> records)
        => records.Select(x => new RecordProjection(
            x.Id,
            x.UserId,
            x.User.Nickname,
            x.MapId,
            x.Map.Name,
            x.Map.Laps,
            x.CarId,
            x.Gravity,
            x.Laps,
            x.Time,
            x.Score,
            x.NbRespawns,
            x.DrivenAt,
            x.SessionId,
            x.Session == null ? null : x.Session.Server.Id,
            x.Session == null ? null : x.Session.Server.Name,
            x.TitleId,
            x.Title == null ? null : x.Title.DisplayName,
            x.GhostId,
            x.Removed));

    internal sealed record RecordProjection(
        int Id,
        string UserLogin,
        string? Nickname,
        string MapUid,
        string MapName,
        int MapLaps,
        string Car,
        int Gravity,
        int Laps,
        int Time,
        int Score,
        int NbRespawns,
        DateTimeOffset DrivenAt,
        Guid? SessionId,
        string? ServerLogin,
        string? ServerName,
        string? TitleId,
        string? TitleDisplayName,
        Guid? GhostId,
        bool Removed)
    {
        public RecordInfo ToRecordInfo(int? rank)
            => new(
                UserLogin,
                Nickname,
                MapUid,
                MapName,
                MapLaps,
                Car,
                Gravity,
                Laps,
                Time,
                Score,
                NbRespawns,
                DrivenAt,
                SessionId,
                ServerLogin,
                ServerName,
                TitleId,
                TitleDisplayName,
                GhostId,
                rank,
                Removed);
    }
}
