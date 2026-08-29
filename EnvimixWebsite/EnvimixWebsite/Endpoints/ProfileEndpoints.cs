using EnvimixWebsite.Services;
using Microsoft.AspNetCore.Mvc;

namespace EnvimixWebsite.Endpoints;

internal static class ProfileEndpoints
{
    public static void Map(WebApplication app)
    {
        app.MapPost("/profile/servers/register", RegisterServer)
            .RequireAuthorization();
    }

    private static async Task<IResult> RegisterServer(
        [FromForm] string serverLogin,
        IEnvimixService envimixService,
        CancellationToken cancellationToken)
    {
        await envimixService.RegisterServerAsync(serverLogin, cancellationToken);
        return TypedResults.LocalRedirect("/profile");
    }
}
