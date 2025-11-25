
namespace EnvimixWebAPI;

internal static class CacheHelper
{
    public static string GetEnvimaniaSessionTokenKey(string token)
    {
        return $"EnvimaniaSessionToken:{token}";
    }

    public static string GetEnvimaniaSessionKey(Guid guid)
    {
        return $"EnvimaniaSession:{guid}";
    }

    public static string GetServerKey(string serverLogin)
    {
        return $"Server:{serverLogin}";
    }

    public static string GetUserKey(string login)
    {
        return $"User:{login}";
    }

    public static string GetManiaPlanetIngameAuthenticationKey(string login, string token)
    {
        return $"ManiaPlanetIngameAuthentication:{login}:{token}";
    }

    public static string GetManiaPlanetOAuth2AuthenticationKey(string token)
    {
        return $"ManiaPlanetOAuth2Authentication:{token}";
    }

    public static string GetZonesKey()
    {
        return "Zones";
    }

    public static string GetZoneKey(string zone)
    {
        return $"Zone:{zone}";
    }

    public static string GetCarsKey()
    {
        return "Cars";
    }

    public static string GetGravityKey()
    {
        return "Gravity";
    }

    public static string GetOfficialRecordsKey(string mapUid, string carName, string zone)
    {
        return $"OfficialRecords:{mapUid}:{carName}:{zone}";
    }

    public static string GetMapRecordsKey(string mapUid, string car, int gravity, int laps, string zone)
    {
        return $"MapRecords:{mapUid}:{car}:{gravity}:{laps}:{zone}";
    }
}

