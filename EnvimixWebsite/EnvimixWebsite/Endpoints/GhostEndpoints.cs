using EnvimixWebsite.Services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace EnvimixWebsite.Endpoints;

internal static class GhostEndpoints
{
    public static void Map(WebApplication app)
    {
        app.MapGet("/ghosts/{ghostId:guid}/download", DownloadGhost);
        app.MapGet("/replays/{replayId:guid}/download", DownloadReplay);
    }

    private static async Task<Results<FileContentHttpResult, NotFound>> DownloadGhost(
        Guid ghostId,
        IEnvimixService envimixService,
        CancellationToken cancellationToken)
    {
        var download = await envimixService.GetGhostAsync(ghostId, cancellationToken);
        return download is null
            ? TypedResults.NotFound()
            : TypedResults.File(download.Data, "application/gbx", download.FileName, enableRangeProcessing: true);
    }

    private static async Task<Results<FileContentHttpResult, NotFound>> DownloadReplay(
        Guid replayId,
        IEnvimixService envimixService,
        CancellationToken cancellationToken)
    {
        var download = await envimixService.GetReplayAsync(replayId, cancellationToken);
        return download is null
            ? TypedResults.NotFound()
            : TypedResults.File(download.Data, "application/gbx", download.FileName, enableRangeProcessing: true);
    }
}
