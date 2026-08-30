using EnvimixWebsite.Services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace EnvimixWebsite.Endpoints;

internal static class GhostEndpoints
{
    public static void Map(WebApplication app)
    {
        app.MapGet("/ghosts/{ghostId:guid}/download", DownloadGhost);
    }

    private static async Task<Results<FileContentHttpResult, NotFound>> DownloadGhost(
        Guid ghostId,
        IEnvimixService envimixService,
        CancellationToken cancellationToken)
    {
        var data = await envimixService.GetGhostAsync(ghostId, cancellationToken);
        return data is null
            ? TypedResults.NotFound()
            : TypedResults.File(data, "application/gbx", $"{ghostId}.Ghost.Gbx", enableRangeProcessing: true);
    }
}
