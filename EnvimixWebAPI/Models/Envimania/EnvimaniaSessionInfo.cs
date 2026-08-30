namespace EnvimixWebAPI.Models.Envimania;

public sealed record EnvimaniaSessionInfo(
    Guid Id,
    string ServerLogin,
    string MapUid,
    string MapName,
    int MapLaps,
    DateTimeOffset StartedAt,
    DateTimeOffset? EndedAt,
    bool FinishedGracefully,
    EnvimaniaSessionRecord[] Records);
