namespace EnvimixWebAPI.Models.Envimania;

public sealed record EnvimaniaServerInfo(
    string ServerLogin,
    string? ServerName,
    int SessionCount,
    int MatchingSessionCount,
    int Page,
    int PageSize,
    DateTimeOffset RegisteredAt,
    DateTimeOffset? LastSeenAt,
    EnvimaniaServerSession[] RecentSessions,
    bool IsHidden,
    bool IsBanned,
    bool CanDelete,
    bool CanAdminister);
