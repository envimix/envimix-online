using EnvimixWebsite.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Security;

namespace EnvimixWebsite.Endpoints;

internal static class TitleEndpoints
{
    public static void Map(WebApplication app)
    {
        app.MapGet("/titles/{titleId}/download", DownloadTitleFile);
        app.MapGet("/titles/{titleId}/download/preview", DownloadTitlePreviewFile);
        app.MapGet("/titles/{titleId}/maniacode", GetTitleManiaCode);
    }

    private static async Task<Results<RedirectHttpResult, NotFound>> DownloadTitleFile(
        string titleId,
        IEnvimixService envimixService,
        CancellationToken cancellationToken)
    {
        var title = await envimixService.GetTitleAsync(titleId, cancellationToken);
        if (title?.Downloadable != true)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Redirect(GetTitleDownloadUrl(titleId), permanent: false);
    }

    private static Results<PhysicalFileHttpResult, NotFound> DownloadTitlePreviewFile(
        string titleId,
        IWebHostEnvironment env)
    {
        if (!string.Equals(titleId, "Envimix_Turbo@bigbang1112", StringComparison.Ordinal))
        {
            return TypedResults.NotFound();
        }

        var fileInfo = env.ContentRootFileProvider.GetFileInfo(Path.Combine("EnvimixTurboPreview", "Envimix_Turbo@bigbang1112.Title.Pack.Gbx"));
        if (!fileInfo.Exists || fileInfo.IsDirectory || fileInfo.PhysicalPath is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.PhysicalFile(fileInfo.PhysicalPath, "application/octet-stream", fileInfo.Name, lastModified: fileInfo.LastModified, enableRangeProcessing: true);
    }

    private static async Task<Results<ContentHttpResult, NotFound>> GetTitleManiaCode(
        string titleId,
        IEnvimixService envimixService,
        CancellationToken cancellationToken)
    {
        var title = await envimixService.GetTitleAsync(titleId, cancellationToken);
        if (title?.Downloadable != true)
        {
            return TypedResults.NotFound();
        }

        var displayName = string.IsNullOrWhiteSpace(title.DisplayName) ? title.Id : title.DisplayName;
        return TypedResults.Content(CreateManiaCode(title.Id, displayName), "application/xml");
    }

    private static string GetTitleDownloadUrl(string titleId)
        => $"https://prod.live.maniaplanet.com/ingame/public/titles/download/{Uri.EscapeDataString(titleId)}.Title.Pack.gbx";

    private static string CreateManiaCode(string titleId, string displayName)
        => $"""
            <?xml version="1.0" encoding="utf-8" ?>
            <maniacode noconfirmation="1">
                <install_pack>
                    <name>{SecurityElement.Escape(displayName)}</name>
                    <file>Packs/{SecurityElement.Escape(titleId)}.Title.Pack.Gbx</file>
                    <url>{SecurityElement.Escape(GetTitleDownloadUrl(titleId))}</url>
                </install_pack>
            </maniacode>
            """;
}
