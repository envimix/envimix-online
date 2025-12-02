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
        ITitleService titleService,
        IUserService userService,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        context.Response.Headers.ETag = $"\"{Guid.NewGuid():n}\"";

        var ratings = await ratingService.GetAveragesByTitleIdAsync(titleId, cancellationToken);
        var stars = await ratingService.GetStarsByTitleIdAsync(titleId, cancellationToken);
        var validations = await envimaniaService.GetValidationsByTitleIdAsync(titleId, cancellationToken);
        var skillpoints = await envimaniaService.GetSkillpointsByTitleId(titleId, cancellationToken);
        var possibleEnvimixCombinations = await envimaniaService.GetPossibleEnvimixCombinationsAsync(titleId, cancellationToken);
        var timeLoginPairsMaps = await envimaniaService.GetTimeLoginPairsByTitleId(titleId, cancellationToken);
        var titleRelease = await titleService.GetTitleReleaseDateAsync(titleId, cancellationToken);

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

        var validatedCount = validations
            .Where(x => x.Gravity == 0)
            .CountBy(x => new { x.MapId, x.CarId });

        var playerSkillpoints = new Dictionary<string, int>();
        var playerActivityPoints = new Dictionary<string, int>();
        var playerCompleted = new Dictionary<string, int>();

        foreach (var (mapUid, timeLoginPairsCombinations) in timeLoginPairsMaps)
        {
            var hasValidation = mappedValidations.ContainsKey(mapUid);

            foreach (var (combination, timeLoginPairs) in timeLoginPairsCombinations)
            {
                var totalRecordCount = timeLoginPairs.Length;

                if (totalRecordCount == 0)
                {
                    continue;
                }

                var worstRanks = timeLoginPairs
                    .Select((x, idx) => new { x.Time, Rank = idx + 1 })
                    .GroupBy(x => x.Time)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Max(x => x.Rank)
                    );

                foreach (var (time, login) in timeLoginPairs)
                {
                    var rank = worstRanks[time];

                    var loginSkillpoints = (int)Math.Ceiling((totalRecordCount - rank) * 100f / rank);

                    var wr = timeLoginPairs[0].Time;
                    var wrPb = wr * 1f / time;
                    var activityPoints = (int)Math.Round(1000 * Math.Exp(totalRecordCount * (wrPb - 1)));

                    if (hasValidation && mappedValidations[mapUid].TryGetValue(combination, out var validation))
                    {
                        if (validation.Login == login && validation.DrivenAt != "" && titleRelease.HasValue)
                        {
                            var validationTimestampInSeconds = long.Parse(validation.DrivenAt);
                            var titlePackReleaseTimestampInSeconds = titleRelease.Value.ToUnixTimeSeconds();
                            var validationAge = validationTimestampInSeconds - titlePackReleaseTimestampInSeconds;
                            var extraActivityPoints = (int)Math.Round(100 + validationAge / 86400f * 10);
                            activityPoints += extraActivityPoints;
                        }
                    }

                    if (!playerSkillpoints.ContainsKey(login))
                    {
                        playerSkillpoints[login] = 0;
                    }
                    playerSkillpoints[login] += loginSkillpoints;

                    if (!playerActivityPoints.ContainsKey(login))
                    {
                        playerActivityPoints[login] = 0;
                    }
                    playerActivityPoints[login] += activityPoints;

                    if (!playerCompleted.ContainsKey(login))
                    {
                        playerCompleted[login] = 0;
                    }
                    playerCompleted[login] += 1;
                }
            }
        }

        var envimixMostSkillpoints = playerSkillpoints
            .OrderByDescending(x => x.Value)
            .ThenBy(x => x.Key)
            .Take(20)
            .Select(x => new PlayerScore
            {
                PlayerLogin = x.Key,
                PlayerNickname = x.Key,
                Score = x.Value
            })
            .ToList();

        var envimixMostActivityPoints = playerActivityPoints
            .OrderByDescending(x => x.Value)
            .ThenBy(x => x.Key)
            .Take(20)
            .Select(x => new PlayerScore
            {
                PlayerLogin = x.Key,
                PlayerNickname = x.Key,
                Score = x.Value
            })
            .ToList();

        var envimixCompletion = playerCompleted
            .OrderByDescending(x => x.Value)
            .ThenBy(x => x.Key)
            .Take(20)
            .Select(x => new PlayerCompletion
            {
                PlayerLogin = x.Key,
                PlayerNickname = x.Key,
                Score = (float)x.Value / possibleEnvimixCombinations
            })
            .ToList();

        var nicknames = await userService.GetNicknamesAsync(envimixMostSkillpoints.Select(x => x.PlayerLogin)
            .Concat(envimixMostActivityPoints.Select(x => x.PlayerLogin))
            .Concat(envimixCompletion.Select(x => x.PlayerLogin))
            .Distinct(), cancellationToken);

        foreach (var player in envimixMostSkillpoints)
        {
            if (nicknames.TryGetValue(player.PlayerLogin, out var nickname))
            {
                player.PlayerNickname = nickname;
            }
        }

        foreach (var player in envimixMostActivityPoints)
        {
            if (nicknames.TryGetValue(player.PlayerLogin, out var nickname))
            {
                player.PlayerNickname = nickname;
            }
        }

        foreach (var player in envimixCompletion)
        {
            if (nicknames.TryGetValue(player.PlayerLogin, out var nickname))
            {
                player.PlayerNickname = nickname;
            }
        }

        return TypedResults.Ok(new TitleStats
        {
            Ratings = ratings,
            Stars = stars,
            Validations = mappedValidations,
            Skillpoints = skillpoints,
            EnvimixOverallCompletion = possibleEnvimixCombinations == 0 ? 0 : (float)validations.Count / possibleEnvimixCombinations,
            EnvimixCompletion = envimixCompletion,
            EnvimixMostSkillpoints = envimixMostSkillpoints,
            EnvimixMostActivityPoints = envimixMostActivityPoints
        });
    }
}
