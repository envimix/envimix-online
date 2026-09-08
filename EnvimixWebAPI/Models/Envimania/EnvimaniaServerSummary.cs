namespace EnvimixWebAPI.Models.Envimania;

public sealed record EnvimaniaServerSummary(
    string ServerLogin,
    string? ServerName,
    int SessionCount,
    string? RegisteredByLogin,
    string? RegisteredByNickname,
    DateTimeOffset? LastSeenAt,
    bool IsHidden,
    bool IsBanned);
