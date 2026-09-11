using EnvimixWebAPI.Entities;
using EnvimixWebAPI.Models;
using EnvimixWebAPI.Security;
using GBX.NET;
using GBX.NET.Engines.Game;
using Microsoft.EntityFrameworkCore;
using System.Threading.Channels;
using System.Xml.Linq;

namespace EnvimixWebAPI.Services;

public interface IGhostSubmissionService
{
    Task<GhostSubmissionResult> SubmitAsync(
        Stream ghostStream,
        string serverLogin,
        string controllerCode,
        CancellationToken cancellationToken);

    Task<GhostAttachmentResult> TryAttachAsync(
        PendingGhostSubmission submission,
        CancellationToken cancellationToken);
}

public sealed class GhostSubmissionService(
    AppDbContext db,
    IModService modService,
    Channel<PendingGhostSubmission> pendingGhostChannel,
    TimeProvider timeProvider,
    ILogger<GhostSubmissionService> logger) : IGhostSubmissionService
{
    public const long MaxGhostSize = 16 * 1024 * 1024;

    public async Task<GhostSubmissionResult> SubmitAsync(
        Stream ghostStream,
        string serverLogin,
        string controllerCode,
        CancellationToken cancellationToken)
    {
        var credentials = await db.Servers
            .Where(x => x.Id == serverLogin && x.DeletedAt == null)
            .Select(x => new { x.ControllerCodeHash, x.ControllerCodeSalt })
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        if (credentials?.ControllerCodeHash is null
            || credentials.ControllerCodeSalt is null
            || !ControllerCodeHasher.Verify(controllerCode, credentials.ControllerCodeHash, credentials.ControllerCodeSalt))
        {
            return GhostSubmissionResult.Unauthorized;
        }

        PendingGhostSubmission submission;
        try
        {
            using var memoryStream = new MemoryStream();
            await ghostStream.CopyToAsync(memoryStream, cancellationToken);
            if (memoryStream.Length is 0 or > MaxGhostSize)
            {
                return GhostSubmissionResult.InvalidGhost;
            }

            memoryStream.Position = 0;
            var ghost = await Gbx.ParseNodeAsync<CGameCtnGhost>(memoryStream, cancellationToken: cancellationToken);
            if (!TryReadMetadata(ghost, out var metadata))
            {
                return GhostSubmissionResult.InvalidGhost;
            }

            submission = new PendingGhostSubmission(
                memoryStream.ToArray(),
                serverLogin,
                ghost.GhostLogin!,
                ghost.Validate_ChallengeUid!,
                metadata.CarId,
                metadata.Laps,
                ghost.RaceTime!.Value.TotalMilliseconds,
                ghost.StuntScore ?? 0,
                ghost.Respawns ?? -1,
                timeProvider.GetUtcNow().AddMinutes(1));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogDebug(ex, "Rejected an invalid ghost submission from server {ServerLogin}.", serverLogin);
            return GhostSubmissionResult.InvalidGhost;
        }

        var attachmentResult = await TryAttachAsync(submission, cancellationToken);
        return attachmentResult switch
        {
            GhostAttachmentResult.Attached => GhostSubmissionResult.Attached,
            GhostAttachmentResult.NotPersonalBest => GhostSubmissionResult.NotPersonalBest,
            GhostAttachmentResult.AlreadyAttached => GhostSubmissionResult.AlreadyAttached,
            _ when pendingGhostChannel.Writer.TryWrite(submission) => GhostSubmissionResult.Queued,
            _ => GhostSubmissionResult.QueueFull
        };
    }

    public async Task<GhostAttachmentResult> TryAttachAsync(
        PendingGhostSubmission submission,
        CancellationToken cancellationToken)
    {
        var matchingRecords = db.Records
            .Where(x => !x.Removed
                && x.Session != null
                && x.Session.Server.Id == submission.ServerLogin
                && x.UserId == submission.PlayerLogin
                && x.MapId == submission.MapUid
                && x.CarId == submission.CarId
                && x.Laps == submission.Laps
                && x.Time == submission.Time
                && x.Score == submission.Score
                && x.NbRespawns == submission.NbRespawns);

        var record = await matchingRecords
            .OrderByDescending(x => x.DrivenAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (record is null)
        {
            return GhostAttachmentResult.RecordNotFound;
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
            return GhostAttachmentResult.NotPersonalBest;
        }

        if (record.ValidationGhostId is not null)
        {
            return GhostAttachmentResult.AlreadyAttached;
        }

        record.ValidationGhost = new GhostEntity { Data = submission.Data };
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Attached submitted ghost to record {RecordId} from server {ServerLogin}.",
            record.Id,
            submission.ServerLogin);
        return GhostAttachmentResult.Attached;
    }

    private bool TryReadMetadata(CGameCtnGhost ghost, out (string CarId, int Laps) metadata)
    {
        var carId = modService.GetCarIdFromPlayerModel(ghost.PlayerModel?.Id);

        if (carId is null
            || string.IsNullOrWhiteSpace(ghost.GhostLogin)
            || string.IsNullOrWhiteSpace(ghost.Validate_ChallengeUid)
            || ghost.RaceTime is null
            || string.IsNullOrWhiteSpace(ghost.Validate_RaceSettings))
        {
            metadata = default;
            return false;
        }

        var raceXml = XDocument.Parse($"<root>{ghost.Validate_RaceSettings}</root>");
        metadata = (carId, (int?)raceXml.Descendants("laps").FirstOrDefault() ?? 0);
        return ghost.RaceTime.Value.TotalMilliseconds > 0;
    }
}
