using EnvimixWebAPI.Models;
using EnvimixWebAPI.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Security.Claims;

namespace EnvimixWebAPI.Endpoints;

public static class TitleEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        group.WithTags("Title Pack");

        group.MapPost("", SubmitTitle);
        group.MapGet("{titleId}/release", GetTitleRelease);
        group.MapGet("{titleId}/stats", GetTitleStats).CacheOutput(x => x.Expire(TimeSpan.FromMinutes(1)).Tag("title-stats"));
    }

    private static async Task<Ok> SubmitTitle(
        TitleSubmitRequest request,
        ITitleService titleService,
        CancellationToken cancellationToken)
    {
        await titleService.SubmitTitleAsync(request, cancellationToken);
        return TypedResults.Ok();
    }

    private static async Task<Results<Ok<TitleReleaseInfo>, NotFound>> GetTitleRelease(
        string titleId,
        ITitleService titleService,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var info = await titleService.GetTitleReleaseInfoAsync(titleId, principal, cancellationToken);

        return info is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(info);
    }

    private static async Task<Results<Ok<TitleStats>, NotFound>> GetTitleStats(
        string titleId, 
        IRatingService ratingService,
        IEnvimaniaService envimaniaService,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        context.Response.Headers.ETag = $"\"{Guid.NewGuid():n}\"";

        var ratings = await ratingService.GetAveragesByTitleIdAsync(titleId, cancellationToken);
        var stars = await ratingService.GetStarsByTitleIdAsync(titleId, cancellationToken);
        var validations = await envimaniaService.GetValidationsByTitleIdAsync(titleId, cancellationToken);
        var skillpoints = await envimaniaService.GetSkillpointsByTitleId(titleId, cancellationToken);

        var mappedValidations = validations.GroupBy(x => x.MapId)
            .ToDictionary(
            g => g.Key,
            g => g.ToDictionary(
                x => $"{x.CarId}_{x.Gravity}_{x.Laps}",
                x => new ValidationInfo 
                { 
                    Login = x.UserId, 
                    Nickname = x.User.Nickname ?? "", 
                    DrivenAt = x.DrivenAt.ToUnixTimeSeconds().ToString(),
                }));

        /*if (principal.Identity?.IsAuthenticated == true && principal.Identity.Name is not null)
        {
            
        }*/

        return TypedResults.Ok(new TitleStats
        {
            Ratings = ratings,
            Stars = stars,
            Validations = mappedValidations,
            Skillpoints = skillpoints
        });
    }
}
