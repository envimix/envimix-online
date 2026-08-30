namespace EnvimixWebAPI.Models.Envimania;

public sealed record EnvimaniaServerInfo(
    string ServerLogin,
    int SessionCount,
    int MatchingSessionCount,
    DateTimeOffset? LastSeenAt,
    EnvimaniaServerSession[] RecentSessions,
    bool IsHidden,
    bool IsBanned,
    bool CanDelete,
    bool CanAdminister);
