using EnvimixWebAPI.Services;

namespace EnvimixWebAPI.Endpoints;

public static class ZoneEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        group.WithTags("Zone");

        group.MapPost("", PostZones).RequireAuthorization(Policies.SuperAdminPolicy);
    }

    private static async Task<IResult> PostZones(IZoneService zoneService, CancellationToken cancellationToken)
    {
        var zones = await zoneService.CreateZonesAsync(cancellationToken);

        return TypedResults.Ok(zones);
    }
}
