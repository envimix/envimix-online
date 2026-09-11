namespace EnvimixWebAPI.Models;

public sealed record PendingReplaySubmission(
    byte[] Data,
    string ServerLogin,
    string PlayerLogin,
    string MapUid,
    string CarId,
    int Laps,
    int Time,
    int Score,
    int NbRespawns,
    DateTimeOffset ExpiresAt);

public enum ReplayAttachmentResult
{
    Attached,
    RecordNotFound,
    NotPersonalBest,
    AlreadyAttached
}

public enum ReplaySubmissionResult
{
    Attached,
    Queued,
    Unauthorized,
    InvalidReplay,
    NotPersonalBest,
    AlreadyAttached,
    QueueFull
}
