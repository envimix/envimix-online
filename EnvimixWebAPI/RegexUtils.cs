using System.Text.RegularExpressions;

namespace EnvimixWebAPI;

internal static partial class RegexUtils
{
    [GeneratedRegex(@"^[A-Z0-9.\-_]+$", RegexOptions.IgnoreCase, matchTimeoutMilliseconds: 500)]
    public static partial Regex LoginRegex();

    [GeneratedRegex(@"^[A-Z0-9]+$", RegexOptions.IgnoreCase, matchTimeoutMilliseconds: 500)]
    public static partial Regex ManiaPlanetTokenRegex();

    [GeneratedRegex(@"^[A-Z0-9_]+$", RegexOptions.IgnoreCase, matchTimeoutMilliseconds: 500)]
    public static partial Regex MapUidRegex();

    [GeneratedRegex(@"^[A-Z0-9-_.]+$", RegexOptions.IgnoreCase, matchTimeoutMilliseconds: 500)]
    public static partial Regex JwtRegex();

    [GeneratedRegex(@"^([A-Z0-9.\-_]{1,50}):([A-Z0-9]{10}$)", RegexOptions.IgnoreCase, matchTimeoutMilliseconds: 500)]
    public static partial Regex IngameAuthRegex();
}