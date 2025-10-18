

using TmEssentials;

namespace EnvimixWebAPI;

static partial class Validator
{
    public static bool ValidateLogin(string? login)
    {
        if (string.IsNullOrWhiteSpace(login))
        {
            return false;
        }

        if (login.Length < 3)
        {
            return false;
        }

        if (login.Length > 100)
        {
            return false;
        }

        if (!RegexUtils.LoginRegex().IsMatch(login))
        {
            return false;
        }

        return true;
    }

    public static bool ValidateManiaPlanetToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        if (token.Length != 10)
        {
            return false;
        }

        if (!RegexUtils.ManiaPlanetTokenRegex().IsMatch(token))
        {
            return false;
        }

        return true;
    }

    public static bool ValidateMapUid(string mapUid)
    {
        if (string.IsNullOrWhiteSpace(mapUid))
        {
            return false;
        }

        if (mapUid.Length < 24)
        {
            return false;
        }

        if (mapUid.Length > 34)
        {
            return false;
        }

        if (!RegexUtils.MapUidRegex().IsMatch(mapUid))
        {
            return false;
        }

        return true;
    }

    public static bool ValidateJwt(string jwt)
    {
        return RegexUtils.JwtRegex().IsMatch(jwt);
    }

    /// <summary>
    /// Warning: Mild validation, don't use with string concatenation.
    /// </summary>
    /// <param name="nickname"></param>
    /// <returns></returns>
    public static bool ValidateNickname(string nickname)
    {
        if (string.IsNullOrWhiteSpace(nickname))
        {
            return false;
        }

        var deformattedNickname = TextFormatter.Deformat(nickname);

        if (deformattedNickname.Length == 0)
        {
            return false;
        }

        if (nickname.Length > 255)
        {
            return false;
        }

        return true;
    }
}