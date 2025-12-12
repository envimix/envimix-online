using Microsoft.AspNetCore.Http.HttpResults;

namespace EnvimixWebsite.Endpoints;

internal class DownloadEndpoints
{
    public static void Map(WebApplication app)
    {
        app.MapGet("/turbo/download", DownloadTurboFile);
        app.MapGet("/turbo/download/preview", DownloadPreviewTurboFile);
    }

    private static RedirectHttpResult DownloadTurboFile()
    {
        return TypedResults.Redirect("https://prod.live.maniaplanet.com/ingame/public/titles/download/Envimix_Turbo@bigbang1112.Title.Pack.gbx", permanent: false);
    }

    private static NotFound DownloadPreviewTurboFile()
    {
        return TypedResults.NotFound();
    }
}
