namespace EnvimixWebAPI.Models.Envimania;

public sealed record EnvimaniaServerInfo(
    string ServerLogin,
    int SessionCount,
    DateTimeOffset? LastSeenAt,
    EnvimaniaServerSession[] RecentSessions,
    bool IsHidden,
    bool IsBanned,
    bool CanDelete,
    bool CanAdminister);
