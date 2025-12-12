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

    private static NotFound DownloadPreviewTurboFile()
    {
        return TypedResults.NotFound();
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
