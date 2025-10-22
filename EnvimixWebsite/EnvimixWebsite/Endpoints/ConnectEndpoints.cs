using AspNet.Security.OAuth.Discord;
using Microsoft.AspNetCore.Http.HttpResults;

namespace EnvimixWebsite.Endpoints;

internal static class ConnectEndpoints
{
    public static void Map(WebApplication app)
    {
        app.MapGet("/connect/discord", ConnectDiscord);
    }

    private static ChallengeHttpResult ConnectDiscord(string? returnUrl = "/")
    {
        return TypedResults.Challenge(new() { RedirectUri = returnUrl }, [DiscordAuthenticationDefaults.AuthenticationScheme]);
    }
}
