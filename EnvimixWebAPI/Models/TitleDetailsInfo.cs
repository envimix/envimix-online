namespace EnvimixWebAPI.Models;

public sealed record TitleDetailsInfo(
    string Id,
    string? DisplayName,
    string? Version,
    DateTimeOffset? ReleasedAt,
    int MapCount,
    int RecordCount,
    int PlayerCount,
    int SessionCount,
    TitleMapInfo[] Maps);

public sealed record TitleMapInfo(
    string Uid,
    string Name,
    string Collection,
    string? Campaign,
    int? Order);
