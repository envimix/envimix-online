using EnvimixWebAPI.Entities;
using EnvimixWebAPI.Services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace EnvimixWebAPI.Endpoints;

public sealed class TotdEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        group.WithTags("Track of the Day");

        group.MapGet("{titleId}", Totd);
    }

    private static async Task<Results<Ok<MapEntity>, ContentHttpResult, NotFound>> Totd(
        string titleId,
        ITotdService totdService,
        CancellationToken cancellationToken)
    {
        if (titleId == "Nadeo_Envimix@bigbang1112")
        {
            var xml = TotdService.GetOldMapXml(titleId);
            return TypedResults.Content(xml, "application/xml");
        }

        var map = await totdService.GetMapAsync(titleId, cancellationToken);

        return map is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(map);
    }
}
