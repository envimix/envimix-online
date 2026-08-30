namespace EnvimixWebAPI.Models.Envimania;

public sealed record EnvimaniaSessionInfo(
    Guid Id,
    string ServerLogin,
    string MapUid,
    string MapName,
    DateTimeOffset StartedAt,
    DateTimeOffset? EndedAt,
    bool FinishedGracefully,
    EnvimaniaSessionRecord[] Records);
