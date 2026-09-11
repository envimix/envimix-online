namespace EnvimixWebAPI.Models;

public sealed record PendingGhostSubmission(
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

public enum GhostAttachmentResult
{
    Attached,
    RecordNotFound,
    NotPersonalBest,
    AlreadyAttached
}

public enum GhostSubmissionResult
{
    Attached,
    Queued,
    Unauthorized,
    InvalidGhost,
    NotPersonalBest,
    AlreadyAttached,
    QueueFull
}
