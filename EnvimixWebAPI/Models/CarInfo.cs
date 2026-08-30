namespace EnvimixWebAPI.Models;

public sealed record CarInfo(
    string Id,
    int RecordCount,
    int PlayerCount,
    RecordInfo[] RecentRecords);
