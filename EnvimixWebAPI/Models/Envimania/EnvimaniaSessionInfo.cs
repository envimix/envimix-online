namespace EnvimixWebAPI.Models.Envimania;

public sealed record EnvimaniaSessionInfo(
    Guid Id,
    string ServerLogin,
    string? ServerName,
    string? ServerModeName,
    string? TitleId,
    string? TitleDisplayName,
    string MapUid,
    string MapName,
    int MapLaps,
    DateTimeOffset StartedAt,
    DateTimeOffset? EndedAt,
    bool FinishedGracefully,
    bool CanAdminister,
    EnvimaniaSessionRecord[] Records);
