using EnvimixWebsite.Services;
using Microsoft.AspNetCore.Mvc;

namespace EnvimixWebsite.Endpoints;

internal static class ServerEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/envimania/servers")
            .RequireAuthorization();

        group.MapPost("/register", RegisterServer);
        group.MapPost("/delete", DeleteServer);
        group.MapPost("/wipe", WipeServer);
        group.MapPost("/delete-records", DeleteServerRecords);
        group.MapPost("/delete-ratings", DeleteServerRatings);
        group.MapPost("/ban", BanServer);
        group.MapPost("/unban", UnbanServer);
    }

    private static async Task<IResult> RegisterServer(
        [FromForm] string serverLogin,
        [FromForm] bool? returnToServer,
        IEnvimixService envimixService,
        CancellationToken cancellationToken)
    {
        await envimixService.RegisterServerAsync(serverLogin, cancellationToken);
        return TypedResults.LocalRedirect(returnToServer == true
            ? $"/envimania/servers/{Uri.EscapeDataString(serverLogin)}"
            : "/profile");
    }

    private static async Task<IResult> DeleteServer(
        [FromForm] string serverLogin,
        IEnvimixService envimixService,
        CancellationToken cancellationToken)
    {
        await envimixService.DeleteServerAsync(serverLogin, cancellationToken);
        return TypedResults.LocalRedirect("/envimania");
    }

    private static async Task<IResult> WipeServer(
        [FromForm] string serverLogin,
        IEnvimixService envimixService,
        CancellationToken cancellationToken)
    {
        await envimixService.WipeServerAsync(serverLogin, cancellationToken);
        return TypedResults.LocalRedirect("/envimania");
    }

    private static async Task<IResult> DeleteServerRecords(
        [FromForm] string serverLogin,
        IEnvimixService envimixService,
        CancellationToken cancellationToken)
    {
        await envimixService.DeleteServerRecordsAsync(serverLogin, cancellationToken);
        return TypedResults.LocalRedirect($"/envimania/servers/{Uri.EscapeDataString(serverLogin)}");
    }

    private static async Task<IResult> DeleteServerRatings(
        [FromForm] string serverLogin,
        IEnvimixService envimixService,
        CancellationToken cancellationToken)
    {
        await envimixService.DeleteServerRatingsAsync(serverLogin, cancellationToken);
        return TypedResults.LocalRedirect($"/envimania/servers/{Uri.EscapeDataString(serverLogin)}");
    }

    private static async Task<IResult> BanServer(
        [FromForm] string serverLogin,
        [FromForm] string reason,
        IEnvimixService envimixService,
        CancellationToken cancellationToken)
    {
        await envimixService.BanServerAsync(serverLogin, reason, cancellationToken);
        return TypedResults.LocalRedirect($"/envimania/servers/{Uri.EscapeDataString(serverLogin)}");
    }

    private static async Task<IResult> UnbanServer(
        [FromForm] string serverLogin,
        IEnvimixService envimixService,
        CancellationToken cancellationToken)
    {
        await envimixService.UnbanServerAsync(serverLogin, cancellationToken);
        return TypedResults.LocalRedirect($"/envimania/servers/{Uri.EscapeDataString(serverLogin)}");
    }
}
