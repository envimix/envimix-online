namespace EnvimixWebAPI.Models;

public sealed record RecordInfo(
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
    string? TitleId,
    string? TitleDisplayName,
    Guid? GhostId,
    int? Rank,
    bool Removed)
{
    public RecordCheckpointInfo[]? Checkpoints { get; init; }
}

public sealed record RecordCheckpointInfo(
    int Time,
    int Score,
    int NbRespawns,
    float Distance,
    float Speed);

public sealed record MapRecordsPage(
    MapRecordCarInfo[] Cars,
    string? Car,
    int Page,
    int PageSize,
    int TotalCount,
    bool ShowAll,
    bool WorldRecordHistory,
    RecordInfo[] Records);

public sealed record MapRecordCarInfo(
    string Id,
    int RecordCount,
    string? ValidatorLogin,
    string? ValidatorNickname);
