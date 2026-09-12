using EnvimixWebAPI.Entities;
using EnvimixWebAPI.Models;
using EnvimixWebAPI.Security;
using GBX.NET;
using GBX.NET.Engines.Game;
using Microsoft.EntityFrameworkCore;
using System.Threading.Channels;
using System.Xml.Linq;

namespace EnvimixWebAPI.Services;

public interface IReplaySubmissionService
{
    Task<ReplaySubmissionResult> SubmitAsync(
        Stream replayStream,
        string serverLogin,
        string controllerCode,
        bool isValidation,
        CancellationToken cancellationToken);

    Task<ReplayAttachmentResult> TryAttachAsync(
        PendingReplaySubmission submission,
        CancellationToken cancellationToken);
}

public sealed class ReplaySubmissionService(
    AppDbContext db,
    IModService modService,
    Channel<PendingReplaySubmission> pendingReplayChannel,
    TimeProvider timeProvider,
    ILogger<ReplaySubmissionService> logger) : IReplaySubmissionService
{
    public const long MaxReplaySize = 64 * 1024 * 1024;

    public async Task<ReplaySubmissionResult> SubmitAsync(
        Stream replayStream,
        string serverLogin,
        string controllerCode,
        bool isValidation,
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
            return ReplaySubmissionResult.Unauthorized;
        }

        PendingReplaySubmission submission;
        try
        {
            using var memoryStream = new MemoryStream();
            await replayStream.CopyToAsync(memoryStream, cancellationToken);
            if (memoryStream.Length is 0 or > MaxReplaySize)
            {
                return ReplaySubmissionResult.InvalidReplay;
            }

            memoryStream.Position = 0;
            var replay = await Gbx.ParseNodeAsync<CGameCtnReplayRecord>(memoryStream, cancellationToken: cancellationToken);
            if (!TryReadMetadata(replay, out var metadata))
            {
                return ReplaySubmissionResult.InvalidReplay;
            }

            submission = new PendingReplaySubmission(
                memoryStream.ToArray(),
                serverLogin,
                replay.PlayerLogin!,
                metadata.MapUid,
                metadata.CarId,
                metadata.Laps,
                metadata.Time,
                metadata.Score,
                metadata.NbRespawns,
                isValidation,
                timeProvider.GetUtcNow().AddMinutes(1));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogDebug(ex, "Rejected an invalid replay submission from server {ServerLogin}.", serverLogin);
            return ReplaySubmissionResult.InvalidReplay;
        }

        var attachmentResult = await TryAttachAsync(submission, cancellationToken);
        return attachmentResult switch
        {
            ReplayAttachmentResult.Attached => ReplaySubmissionResult.Attached,
            ReplayAttachmentResult.NotPersonalBest => ReplaySubmissionResult.NotPersonalBest,
            ReplayAttachmentResult.AlreadyAttached => ReplaySubmissionResult.AlreadyAttached,
            _ when pendingReplayChannel.Writer.TryWrite(submission) => ReplaySubmissionResult.Queued,
            _ => ReplaySubmissionResult.QueueFull
        };
    }

    public async Task<ReplayAttachmentResult> TryAttachAsync(
        PendingReplaySubmission submission,
        CancellationToken cancellationToken)
    {
        var record = await db.Records
            .Where(x => !x.Removed
                && x.Session != null
                && x.Session.Server.Id == submission.ServerLogin
                && x.UserId == submission.PlayerLogin
                && x.MapId == submission.MapUid
                && x.CarId == submission.CarId
                && x.Laps == submission.Laps
                && x.Time == submission.Time
                && x.Score == submission.Score
                && x.NbRespawns == submission.NbRespawns)
            .OrderByDescending(x => x.DrivenAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (record is null)
        {
            return ReplayAttachmentResult.RecordNotFound;
        }

        var hasBetterRecord = await db.Records.AnyAsync(x => !x.Removed
            && x.UserId == record.UserId
            && x.MapId == record.MapId
            && x.CarId == record.CarId
            && x.Gravity == record.Gravity
            && x.Laps == record.Laps
            && x.Time < record.Time,
            cancellationToken);
        if (hasBetterRecord)
        {
            return ReplayAttachmentResult.NotPersonalBest;
        }

        if (submission.IsValidation ? record.ValidationReplayId is not null : record.ReplayId is not null)
        {
            return ReplayAttachmentResult.AlreadyAttached;
        }

        if (submission.IsValidation)
        {
            record.ValidationReplay = new ReplayEntity { Data = submission.Data };
        }
        else
        {
            record.Replay = new ReplayEntity { Data = submission.Data };
        }
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Attached submitted {ReplayKind} replay to record {RecordId} from server {ServerLogin}.",
            submission.IsValidation ? "validation" : "main",
            record.Id,
            submission.ServerLogin);
        return ReplayAttachmentResult.Attached;
    }

    private bool TryReadMetadata(
        CGameCtnReplayRecord replay,
        out (string MapUid, string CarId, int Laps, int Time, int Score, int NbRespawns) metadata)
    {
        if (string.IsNullOrWhiteSpace(replay.PlayerLogin) || string.IsNullOrWhiteSpace(replay.MapInfo?.Id))
        {
            metadata = default;
            return false;
        }

        var matchingGhosts = replay.Ghosts?
            .Where(x => x.GhostLogin == replay.PlayerLogin && x.Validate_ChallengeUid == replay.MapInfo.Id)
            .ToArray() ?? [];
        if (matchingGhosts.Length != 1)
        {
            metadata = default;
            return false;
        }

        var ghost = matchingGhosts[0];
        var carId = modService.GetCarIdFromPlayerModel(ghost.PlayerModel?.Id);
        if (carId is null || ghost.RaceTime is null || string.IsNullOrWhiteSpace(ghost.Validate_RaceSettings))
        {
            metadata = default;
            return false;
        }

        var raceXml = XDocument.Parse($"<root>{ghost.Validate_RaceSettings}</root>");
        metadata = (
            replay.MapInfo.Id,
            carId,
            (int?)raceXml.Descendants("laps").FirstOrDefault() ?? 0,
            ghost.RaceTime.Value.TotalMilliseconds,
            ghost.StuntScore ?? 0,
            ghost.Respawns ?? -1);
        return metadata.Time > 0;
    }
}
