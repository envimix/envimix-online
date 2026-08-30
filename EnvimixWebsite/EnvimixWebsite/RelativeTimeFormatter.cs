namespace EnvimixWebsite;

public static class RelativeTimeFormatter
{
    public static string Format(DateTimeOffset timestamp, DateTimeOffset? now = null)
    {
        var difference = (now ?? DateTimeOffset.UtcNow) - timestamp;
        var future = difference < TimeSpan.Zero;
        var duration = difference.Duration();

        var relative = duration switch
        {
            { TotalSeconds: < 60 } => "just now",
            { TotalMinutes: < 60 } => FormatUnit((int)duration.TotalMinutes, "minute"),
            { TotalHours: < 24 } => FormatUnit((int)duration.TotalHours, "hour"),
            { TotalDays: < 30 } => FormatUnit((int)duration.TotalDays, "day"),
            { TotalDays: < 365 } => FormatUnit((int)(duration.TotalDays / 30), "month"),
            _ => FormatUnit((int)(duration.TotalDays / 365), "year")
        };

        return relative == "just now" ? relative : future ? $"in {relative}" : $"{relative} ago";
    }

    private static string FormatUnit(int value, string unit)
        => $"{value} {unit}{(value == 1 ? "" : "s")}";
}
