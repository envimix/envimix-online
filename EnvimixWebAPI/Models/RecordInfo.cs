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
    Guid? GhostId,
    int? Rank,
    bool Removed);

public sealed record MapRecordsPage(
    MapRecordCarInfo[] Cars,
    string? Car,
    int Page,
    int PageSize,
    int TotalCount,
    bool ShowAll,
    RecordInfo[] Records);

public sealed record MapRecordCarInfo(
    string Id,
    int RecordCount,
    string? ValidatorLogin,
    string? ValidatorNickname);
