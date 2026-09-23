using EnvimixWebAPI.Entities;
using GBX.NET;
using GBX.NET.Engines.Game;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using System.Data;
using System.Xml.Linq;

namespace EnvimixWebAPI.Services;

public interface ISessionRecordRestorationService
{
    Task<string?> RestoreAsync(Guid sessionId, Stream replayStream, DateTimeOffset fileLastModified, DateTimeOffset? drivenAtOverride, int gravity, CancellationToken cancellationToken);
}

public sealed class SessionRecordRestorationService(
    AppDbContext db,
    IModService modService,
    IOutputCacheStore outputCache,
    HybridCache hybridCache,
    ILogger<SessionRecordRestorationService> logger) : ISessionRecordRestorationService
{
    public async Task<string?> RestoreAsync(Guid sessionId, Stream replayStream, DateTimeOffset fileLastModified, DateTimeOffset? drivenAtOverride, int gravity, CancellationToken cancellationToken)
    {
        var session = await db.EnvimaniaSessions
            .Include(x => x.Map)
            .FirstOrDefaultAsync(x => x.Id == sessionId, cancellationToken);
        if (session is null)
        {
            return "Session not found.";
        }

        using var memory = new MemoryStream();
        CGameCtnReplayRecord replay;
        CGameCtnGhost ghost;
        string carId;
        int laps;
        int time;
        try
        {
            await replayStream.CopyToAsync(memory, cancellationToken);
            if (memory.Length is 0 or > ReplaySubmissionService.MaxReplaySize)
            {
                return "Replay must be between 1 byte and 64 MB.";
            }

            memory.Position = 0;
            replay = await Gbx.ParseNodeAsync<CGameCtnReplayRecord>(memory, cancellationToken: cancellationToken);
            if (string.IsNullOrWhiteSpace(replay.PlayerLogin) || replay.MapInfo?.Id != session.Map.Id)
            {
                return "Replay player or map does not match this session.";
            }

            var matchingGhosts = replay.Ghosts?.Where(x => x.GhostLogin == replay.PlayerLogin).ToArray() ?? [];
            if (matchingGhosts.Length != 1)
            {
                return "Replay must contain exactly one ghost for its player.";
            }

            ghost = matchingGhosts[0];
            var parsedCarId = modService.GetCarIdFromPlayerModel(ghost.PlayerModel?.Id);
            if (parsedCarId is null || !modService.IsValidCar(parsedCarId) || string.IsNullOrWhiteSpace(ghost.Validate_RaceSettings))
            {
                return "Replay car or race settings are invalid.";
            }
            carId = parsedCarId;

            var settings = XDocument.Parse($"<root>{ghost.Validate_RaceSettings}</root>");
            laps = (int?)settings.Descendants("laps").FirstOrDefault() ?? 0;
            time = ghost.EventsDuration.TotalMilliseconds;
            if (time <= 0 || laps < 0)
            {
                return "Replay time or lap count is invalid.";
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Could not parse replay for session {SessionId}.", sessionId);
            return "The uploaded file is not a valid replay.";
        }

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var replayRespawns = ghost.Respawns;
        var candidates = await db.Records
            .Include(x => x.Replay)
            .Where(x => x.SessionId == sessionId && x.UserId == replay.PlayerLogin
                && x.MapId == session.Map.Id && x.CarId == carId
                && x.Laps == laps && x.Time == time)
            .ToArrayAsync(cancellationToken);
        var matches = replayRespawns is int respawns
            ? candidates.Where(x => x.NbRespawns == respawns).ToArray()
            : candidates;
        if (candidates.Length > 0 && matches.Length == 0)
        {
            return "An existing session record matches this replay except for respawns; no record was changed.";
        }
        if (matches.Length > 1)
        {
            return "Multiple session records match this replay; no record was changed.";
        }

        var patchExisting = matches.Length == 1;
        RecordEntity record;
        if (patchExisting)
        {
            record = matches[0];
            await db.Checkpoints.Where(x => x.Record.Id == record.Id).ExecuteDeleteAsync(cancellationToken);
            if (record.Replay is null)
            {
                record.Replay = new ReplayEntity { Data = memory.ToArray() };
            }
            else
            {
                record.Replay.Data = memory.ToArray();
                record.Replay.LastModifiedAt = DateTimeOffset.UtcNow;
            }
        }
        else
        {
            if (!modService.IsValidGravity(gravity))
            {
                return "Invalid gravity for a new record.";
            }

            var drivenAt = drivenAtOverride ?? ghost.WalltimeEndTimestamp ?? fileLastModified;
            var latest = session.EndedAt ?? DateTimeOffset.UtcNow;
            if (drivenAt < session.StartedAt.AddMinutes(-10) || drivenAt > latest.AddMinutes(10))
            {
                return "Replay date is outside this session.";
            }

            var user = await db.Users.FirstOrDefaultAsync(x => x.Id == replay.PlayerLogin, cancellationToken);
            if (user is null)
            {
                return "Replay player is not registered in Envimix.";
            }

            record = new RecordEntity
            {
                User = user,
                Map = session.Map,
                Car = await modService.GetOrAddCarAsync(carId, cancellationToken),
                Gravity = gravity,
                DrivenAt = drivenAt,
                ServersideDrivenAt = drivenAt,
                Session = session,
                TitleId = session.TitleId,
                Laps = laps,
                Time = time,
                Score = ghost.StuntScore ?? 0,
                NbRespawns = ghost.Respawns ?? -1,
                Replay = new ReplayEntity { Data = memory.ToArray() },
                Restored = true
            };
            await db.Records.AddAsync(record, cancellationToken);
        }

        foreach (var checkpoint in ghost.Checkpoints ?? [])
        {
            var checkpointTime = checkpoint.Time.GetValueOrDefault().TotalMilliseconds;
            if (checkpointTime < 0 || checkpointTime >= record.Time)
            {
                continue;
            }
            record.Checkpoints.Add(new CheckpointEntity
            {
                Record = record,
                Time = checkpointTime,
                Score = checkpoint.StuntsScore ?? 0,
                NbRespawns = ghost.Respawns ?? -1,
                Distance = -1,
                Speed = -1
            });
        }
        record.Checkpoints.Add(new CheckpointEntity
        {
            Record = record,
            Time = time,
            Score = record.Score,
            NbRespawns = record.NbRespawns,
            Distance = -1,
            Speed = -1
        });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        try
        {
            await outputCache.EvictByTagAsync("title-stats", cancellationToken);
            await hybridCache.RemoveAsync(CacheHelper.GetMapRecordsKey(session.Map.Id, carId, record.Gravity, laps, "World"), cancellationToken);
            if (session.Map.TitlePackId is string titleId)
            {
                await hybridCache.RemoveAsync($"PlayerRecordsByTitleId_{titleId}", cancellationToken);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Could not clear record caches after restoring record {RecordId}.", record.Id);
        }
        logger.LogInformation("{Action} record {RecordId} for player {PlayerLogin} in session {SessionId} from replay.", patchExisting ? "Patched" : "Restored", record.Id, record.UserId, sessionId);
        return null;
    }
}
