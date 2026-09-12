using EnvimixWebAPI.Entities;
using EnvimixWebAPI.Models;
using EnvimixWebAPI.Security;
using GBX.NET;
using GBX.NET.Engines.Game;
using Microsoft.EntityFrameworkCore;

namespace EnvimixWebAPI.Services;

public interface ISessionReplaySubmissionService
{
    Task<SessionReplaySubmissionResult> SubmitAsync(
        Stream replayStream,
        string serverLogin,
        string controllerCode,
        CancellationToken cancellationToken);
}

public sealed class SessionReplaySubmissionService(AppDbContext db) : ISessionReplaySubmissionService
{
    public async Task<SessionReplaySubmissionResult> SubmitAsync(
        Stream replayStream,
        string serverLogin,
        string controllerCode,
        CancellationToken cancellationToken)
    {
        var credentials = await db.Servers
            .Where(x => x.Id == serverLogin && x.DeletedAt == null && x.BanReason == null)
            .Select(x => new { x.ControllerCodeHash, x.ControllerCodeSalt })
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        if (credentials?.ControllerCodeHash is null
            || credentials.ControllerCodeSalt is null
            || !ControllerCodeHasher.Verify(controllerCode, credentials.ControllerCodeHash, credentials.ControllerCodeSalt))
        {
            return SessionReplaySubmissionResult.Unauthorized;
        }

        using var memoryStream = new MemoryStream();
        try
        {
            await replayStream.CopyToAsync(memoryStream, cancellationToken);
            if (memoryStream.Length is 0 or > ReplaySubmissionService.MaxReplaySize)
            {
                return SessionReplaySubmissionResult.InvalidReplay;
            }

            memoryStream.Position = 0;
            var replay = await Gbx.ParseNodeAsync<CGameCtnReplayRecord>(memoryStream, cancellationToken: cancellationToken);
            if (string.IsNullOrWhiteSpace(replay.MapInfo?.Id))
            {
                return SessionReplaySubmissionResult.InvalidReplay;
            }

            var session = await db.EnvimaniaSessions
                .Where(x => x.Server.Id == serverLogin)
                .OrderByDescending(x => x.StartedAt)
                .Select(x => new { x.Id, MapUid = x.Map.Id, x.ReplayId })
                .FirstOrDefaultAsync(cancellationToken);
            if (session is null)
            {
                return SessionReplaySubmissionResult.SessionNotFound;
            }

            if (session.MapUid != replay.MapInfo.Id)
            {
                return SessionReplaySubmissionResult.InvalidReplay;
            }

            if (session.ReplayId is not null)
            {
                return SessionReplaySubmissionResult.AlreadySubmitted;
            }

            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            var replayEntity = new ReplayEntity { Data = memoryStream.ToArray() };
            await db.Replays.AddAsync(replayEntity, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);

            var attached = await db.EnvimaniaSessions
                .Where(x => x.Id == session.Id && x.ReplayId == null)
                .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.ReplayId, replayEntity.Id), cancellationToken);
            if (attached == 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                return SessionReplaySubmissionResult.AlreadySubmitted;
            }

            await transaction.CommitAsync(cancellationToken);
            return SessionReplaySubmissionResult.Submitted;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return SessionReplaySubmissionResult.InvalidReplay;
        }
    }
}
