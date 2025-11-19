using EnvimixWebAPI.Models;
using EnvimixWebAPI.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Caching.Hybrid;

namespace EnvimixWebAPI.Endpoints;

public static class TotdEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        group.WithTags("Track of the Day");

        group.MapGet("{titleId}", Totd);
    }

    private static async Task<Results<Ok<TotdInfo>, ContentHttpResult, NotFound>> Totd(
        string titleId,
        ITotdService totdService,
        ITitleService titleService,
        HybridCache hybridCache,
        CancellationToken cancellationToken)
    {
        if (titleId == "Nadeo_Envimix@bigbang1112")
        {
            var xml = await hybridCache.GetOrCreateAsync("Totd_Nadeo_Envimix@bigbang1112", _ =>
            {
                return ValueTask.FromResult(TotdService.GetOldMapXml(titleId));
            }, new() { Expiration = DateTime.UtcNow.Date.AddDays(1) - DateTime.UtcNow }, cancellationToken: cancellationToken); 

            return TypedResults.Content(xml, "application/xml");
        }

        var totd = await hybridCache.GetOrCreateAsync($"Totd_{titleId}", async _ =>
        {
            var titleReleaseDate = await titleService.GetTitleReleaseDateAsync(titleId, cancellationToken);

            if (titleReleaseDate is null)
            {
                return null;
            }

            if (titleReleaseDate > DateTimeOffset.UtcNow)
            {
                return null;
            }

            var map = await totdService.GetMapAsync(titleId, cancellationToken);

            if (map is null)
            {
                return null;
            }

            return new TotdInfo
            {
                Map = map,
                NextAt = new DateTimeOffset(DateTime.UtcNow.Date.AddDays(1)).ToUnixTimeSeconds().ToString()
            };
        }, new() { Expiration = DateTime.UtcNow.Date.AddDays(1) - DateTime.UtcNow }, cancellationToken: cancellationToken);

        return totd is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(totd);
    }
}
