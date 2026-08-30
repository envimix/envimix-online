namespace EnvimixWebAPI.Models.Envimania;

public sealed record EnvimaniaServerSummary(
    string ServerLogin,
    int SessionCount,
    DateTimeOffset? LastSeenAt,
    bool IsHidden,
    bool IsBanned);

public sealed record EnvimaniaServerInfo(
    string ServerLogin,
    int SessionCount,
    DateTimeOffset? LastSeenAt,
    EnvimaniaServerSession[] RecentSessions,
    bool IsHidden,
    bool IsBanned,
    bool CanDelete,
    bool CanAdminister);

public sealed record EnvimaniaServerSession(
    Guid Id,
    string MapUid,
    string MapName,
    DateTimeOffset StartedAt,
    DateTimeOffset? EndedAt);

public sealed record EnvimaniaSessionInfo(
    Guid Id,
    string ServerLogin,
    string MapUid,
    string MapName,
    DateTimeOffset StartedAt,
    DateTimeOffset? EndedAt,
    EnvimaniaSessionRecord[] Records);

public sealed record EnvimaniaSessionRecord(
    string UserLogin,
    string? Nickname,
    string Car,
    int Laps,
    int Time,
    int Score,
    int NbRespawns,
    DateTimeOffset DrivenAt);
