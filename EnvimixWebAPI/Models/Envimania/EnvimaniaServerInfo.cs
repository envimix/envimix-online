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
    string MapUid,
    string MapName,
    DateTimeOffset StartedAt,
    DateTimeOffset? EndedAt);
