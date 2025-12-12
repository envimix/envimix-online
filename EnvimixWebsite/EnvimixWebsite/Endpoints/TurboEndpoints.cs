using Microsoft.AspNetCore.Http.HttpResults;

namespace EnvimixWebsite.Endpoints;

internal static class TurboEndpoints
{
    private const string StableDownloadUrl = "https://prod.live.maniaplanet.com/ingame/public/titles/download/Envimix_Turbo@bigbang1112.Title.Pack.gbx";

    public static void Map(WebApplication app)
    {
        app.MapGet("/turbo/download", DownloadTurboFile);
        app.MapGet("/turbo/download/preview", DownloadPreviewTurboFile);
        app.MapGet("/turbo/maniacode", GetManiaCode);
    }

    private static RedirectHttpResult DownloadTurboFile()
    {
        return TypedResults.Redirect(StableDownloadUrl, permanent: false);
    }

    private static Results<PhysicalFileHttpResult, NotFound> DownloadPreviewTurboFile(IWebHostEnvironment env)
    {
        var fileInfo = env.ContentRootFileProvider.GetFileInfo(Path.Combine("EnvimixTurboPreview", "Envimix_Turbo@bigbang1112.Title.Pack.Gbx"));

        if (!fileInfo.Exists || fileInfo.IsDirectory || fileInfo.PhysicalPath is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.PhysicalFile(fileInfo.PhysicalPath, "application/octet-stream", fileInfo.Name, lastModified: fileInfo.LastModified, enableRangeProcessing: true);
    }

    private static ContentHttpResult GetManiaCode()
    {
        var maniaCodeContent = $"""
            ﻿<?xml version="1.0" encoding="utf-8" ?>
            <maniacode noconfirmation="1">
                <install_pack>
                    <name>$fffENVIMIX $ff0Turbo</name>
                    <file>Packs/Envimix_Turbo@bigbang1112.Title.Pack.Gbx</file>
                    <url>{StableDownloadUrl}</url>
                </install_pack>
            </maniacode>
            """;

        return TypedResults.Content(maniaCodeContent, "application/xml");
    }
}
