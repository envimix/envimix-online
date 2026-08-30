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
    bool Removed);
