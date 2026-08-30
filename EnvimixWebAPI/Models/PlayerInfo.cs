namespace EnvimixWebAPI.Models;

public sealed record PlayerInfo(
    string Login,
    string? Nickname,
    string? Zone,
    int RecordCount,
    RecordInfo[] RecentRecords);
