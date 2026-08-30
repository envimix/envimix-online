namespace EnvimixWebAPI.Models.Envimania;

public sealed record EnvimaniaServerSummary(
    string ServerLogin,
    string? ServerName,
    int SessionCount,
    DateTimeOffset? LastSeenAt,
    bool IsHidden,
    bool IsBanned);
