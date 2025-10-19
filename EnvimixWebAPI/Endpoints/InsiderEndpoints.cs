using EnvimixWebAPI.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace EnvimixWebAPI.Endpoints;

public static class InsiderEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        group.WithTags("Insider");

        group.MapGet("", Insiders)
            .RequireAuthorization(Policies.SuperAdminPolicy)
            .CacheOutput();

        group.MapGet("{id}", InsiderById).CacheOutput();

        group.MapPost("", AddInsiders).RequireAuthorization(Policies.SuperAdminPolicy);
    }

    private static async Task<Ok<List<string>>> Insiders(IInsiderService insiderService, CancellationToken cancellationToken)
    {
        var insiders = await insiderService.GetAllUserIdsAsync(cancellationToken);

        return TypedResults.Ok(insiders);
    }

    private static async Task<Results<Ok<string>, NotFound>> InsiderById(string id, IInsiderService insiderService, CancellationToken cancellationToken)
    {
        var insider = await insiderService.GetByUserIdAsync(id, cancellationToken);

        return insider is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(insider);
    }

    private static async Task<Ok> AddInsiders([FromBody] string[] insiders, IInsiderService insiderService, CancellationToken cancellationToken)
    {
        _ = await insiderService.AddInsidersAsync(insiders, cancellationToken);

        return TypedResults.Ok();
    }
}
