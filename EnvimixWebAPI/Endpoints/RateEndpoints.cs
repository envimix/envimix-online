using EnvimixWebAPI.Models;
using EnvimixWebAPI.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EnvimixWebAPI.Endpoints;

public static class RateEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        group.WithTags("Rate");

        group.MapPost("", Rate)
            .RequireAuthorization(Policies.ManiaPlanetUserPolicy);
        group.MapPost("star", RateStar)
            .RequireAuthorization(Policies.AdminPolicy);
        group.MapPost("unstar", RateUnstar)
            .RequireAuthorization(Policies.AdminPolicy);
    }

    private static async Task<IResult> Rate(
        [FromBody] RatingClientRequest request,
        IRatingService ratingService,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var result = await ratingService.SetAsync(request, principal, cancellationToken);

        return result.Match<IResult>(
            validResponse => TypedResults.Ok(validResponse),
            validationFailure => TypedResults.BadRequest(validationFailure),
            actionForbidden => TypedResults.Forbid()
        );
    }

    private static async Task<IResult> RateStar(
        [FromBody] RatingStarRequest request,
        IStarService starService,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var result = await starService.StarAsync(request, principal, cancellationToken);

        return result.Match<IResult>(
            validResponse => TypedResults.Ok(),
            validationFailure => TypedResults.BadRequest(validationFailure)
        );
    }

    private static async Task<IResult> RateUnstar(
        [FromBody] RatingStarRequest request,
        IStarService starService,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var result = await starService.UnstarAsync(request, principal, cancellationToken);

        return result.Match<IResult>(
            validResponse => TypedResults.Ok(),
            validationFailure => TypedResults.BadRequest(validationFailure)
        );
    }
}
