namespace EnvimixWebAPI.Models.Envimania;

public sealed record EnvimaniaServerSession(
    Guid Id,
    string MapUid,
    string MapName,
    string? ServerModeName,
    string? TitleId,
    DateTimeOffset StartedAt,
    DateTimeOffset? EndedAt,
    bool FinishedGracefully,
    int PlayerCount);
