using EnvimixWebAPI.Services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace EnvimixWebAPI.Endpoints;

public static class ZoneEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        group.WithTags("Zone");

        group.MapPost("", PostZones).RequireAuthorization(Policies.SuperAdminPolicy);
    }

    private static async Task<Ok<IEnumerable<string>>> PostZones(IZoneService zoneService, CancellationToken cancellationToken)
    {
        var zones = await zoneService.CreateZonesAsync(cancellationToken);

        return TypedResults.Ok(zones);
    }
}
