using EnvimixWebAPI.Models;
using EnvimixWebAPI.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Security.Claims;
using static GBX.NET.Engines.Game.CGameCtnChallenge;

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
        IStarService starService,
        IEnvimaniaService envimaniaService,
        ITitleService titleService,
        IUserService userService,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        context.Response.Headers.ETag = $"\"{Guid.NewGuid():n}\"";

        var ratings = await ratingService.GetAveragesByTitleIdAsync(titleId, cancellationToken);
        var stars = await starService.GetStarsByTitleIdAsync(titleId, cancellationToken);
        var validations = await envimaniaService.GetValidationsByTitleIdAsync(titleId, cancellationToken);
        var playerRecords = await envimaniaService.GetPlayerRecordsByTitleId(titleId, cancellationToken);
        var totalCombinations = await envimaniaService.GetTotalCombinationsAsync(titleId, cancellationToken);
        var titleRelease = await titleService.GetTitleReleaseDateAsync(titleId, cancellationToken);

        var playerEnvimixSkillpoints = new Dictionary<string, int>();
        var playerEnvimixActivityPoints = new Dictionary<string, int>();
        var playerEnvimixCompleted = new Dictionary<string, int>();
        var playerDefaultCarSkillpoints = new Dictionary<string, int>();
        var playerDefaultCarActivityPoints = new Dictionary<string, int>();
        var playerDefaultCarCompleted = new Dictionary<string, int>();

        var envimixValidationCount = 0;
        var defaultCarValidationCount = 0;

        var combinations = new Dictionary<string, CombinationStat>();
        foreach (var validation in validations)
        {
            var timeLoginPairs = playerRecords[$"{validation.MapId}_{validation.CarId}_{validation.Gravity}_{validation.Laps}"].OrderBy(x => x.Time).ToArray();

            var skillpoints = timeLoginPairs
                .GroupBy(x => x.Time)
                .SelectMany(g => new[] { g.Key, g.Count() })
                .ToArray();

            var rating = ratings.GetValueOrDefault(validation.MapId)?
                .GetValueOrDefault($"{validation.CarId}_{validation.Gravity}_Time");

            var isDefaultCar = validation.IsDefaultCar();

            if (isDefaultCar)
            {
                defaultCarValidationCount++;
            }
            else
            {
                envimixValidationCount++;
            }

            combinations[$"{validation.MapId}_{validation.CarId}_{validation.Gravity}"] = new CombinationStat
            {
                ValidationLogin = isDefaultCar ? "" : validation.UserId,
                ValidationDrivenAt = isDefaultCar ? "" : validation.DrivenAt.ToUnixTimeSeconds().ToString(),
                Difficulty = rating?.Difficulty ?? -1,
                Quality = rating?.Quality ?? -1,
                Skillpoints = skillpoints
            };

            var totalRecordCount = timeLoginPairs.Length;

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

                if (!isDefaultCar && validation.UserId == login && titleRelease.HasValue)
                {
                    var validationTimestampInSeconds = validation.DrivenAt.ToUnixTimeSeconds();
                    var titlePackReleaseTimestampInSeconds = titleRelease.Value.ToUnixTimeSeconds();
                    var validationAge = validationTimestampInSeconds - titlePackReleaseTimestampInSeconds;
                    var extraActivityPoints = (int)Math.Round(100 + validationAge / 86400f * 10);
                    activityPoints += extraActivityPoints;
                }

                if (isDefaultCar)
                {
                    if (!playerDefaultCarSkillpoints.ContainsKey(login))
                    {
                        playerDefaultCarSkillpoints[login] = 0;
                    }
                    playerDefaultCarSkillpoints[login] += loginSkillpoints;

                    if (!playerDefaultCarActivityPoints.ContainsKey(login))
                    {
                        playerDefaultCarActivityPoints[login] = 0;
                    }
                    playerDefaultCarActivityPoints[login] += activityPoints;

                    if (!playerDefaultCarCompleted.ContainsKey(login))
                    {
                        playerDefaultCarCompleted[login] = 0;
                    }
                    playerDefaultCarCompleted[login] += 1;
                }
                else
                {
                    if (!playerEnvimixSkillpoints.ContainsKey(login))
                    {
                        playerEnvimixSkillpoints[login] = 0;
                    }
                    playerEnvimixSkillpoints[login] += loginSkillpoints;

                    if (!playerEnvimixActivityPoints.ContainsKey(login))
                    {
                        playerEnvimixActivityPoints[login] = 0;
                    }
                    playerEnvimixActivityPoints[login] += activityPoints;

                    if (!playerEnvimixCompleted.ContainsKey(login))
                    {
                        playerEnvimixCompleted[login] = 0;
                    }
                    playerEnvimixCompleted[login] += 1;
                }
            }
        }

        // NEW RULE: unfinished combinations cannot be rated

        var envimixMostSkillpoints = playerEnvimixSkillpoints
            .OrderByDescending(x => x.Value)
            .ThenBy(x => x.Key)
            .Select(x => new PlayerScore
            {
                Login = x.Key,
                Score = x.Value
            })
            .ToList();

        var envimixMostActivityPoints = playerEnvimixActivityPoints
            .OrderByDescending(x => x.Value)
            .ThenBy(x => x.Key)
            .Select(x => new PlayerScore
            {
                Login = x.Key,
                Score = x.Value
            })
            .ToList();

        var envimixCompletion = playerEnvimixCompleted
            .OrderByDescending(x => x.Value)
            .ThenBy(x => x.Key)
            .Select(x => new PlayerCompletion
            {
                Login = x.Key,
                Score = (float)x.Value / totalCombinations.EnvimixCount
            })
            .ToList();

        var defaultCarMostSkillpoints = playerDefaultCarSkillpoints
            .OrderByDescending(x => x.Value)
            .ThenBy(x => x.Key)
            .Select(x => new PlayerScore
            {
                Login = x.Key,
                Score = x.Value
            })
            .ToList();

        var defaultCarMostActivityPoints = playerDefaultCarActivityPoints
            .OrderByDescending(x => x.Value)
            .ThenBy(x => x.Key)
            .Select(x => new PlayerScore
            {
                Login = x.Key,
                Score = x.Value
            })
            .ToList();

        var defaultCarCompletion = playerDefaultCarCompleted
            .OrderByDescending(x => x.Value)
            .ThenBy(x => x.Key)
            .Select(x => new PlayerCompletion
            {
                Login = x.Key,
                Score = (float)x.Value / totalCombinations.DefaultCarCount
            })
            .ToList();

        var globalMostSkillpoints = playerEnvimixSkillpoints
            .Concat(playerDefaultCarSkillpoints)
            .GroupBy(x => x.Key)
            .Select(g => new PlayerScore
            {
                Login = g.Key,
                Score = g.Sum(x => x.Value)
            })
            .OrderByDescending(x => x.Score)
            .ToList();

        var globalMostActivityPoints = playerEnvimixActivityPoints
            .Concat(playerDefaultCarActivityPoints)
            .GroupBy(x => x.Key)
            .Select(g => new PlayerScore
            {
                Login = g.Key,
                Score = g.Sum(x => x.Value)
            })
            .OrderByDescending(x => x.Score)
            .ToList();

        var globalCompletion = playerEnvimixCompleted
            .Concat(playerDefaultCarCompleted)
            .GroupBy(x => x.Key)
            .Select(g => new PlayerCompletion
            {
                Login = g.Key,
                Score = (float)g.Sum(x => x.Value) / totalCombinations.TotalCount
            })
            .OrderByDescending(x => x.Score)
            .ToList();

        var players = await userService.GetTitleUserInfosAsync(globalCompletion.Select(x => x.Login), cancellationToken);

        return TypedResults.Ok(new TitleStats
        {
            EnvimixCompletionPercentage = totalCombinations.EnvimixCount == 0 ? 0 : (float)envimixValidationCount / totalCombinations.EnvimixCount,
            DefaultCarCompletionPercentage = totalCombinations.DefaultCarCount == 0 ? 0 : (float)defaultCarValidationCount / totalCombinations.DefaultCarCount,
            GlobalCompletionPercentage = totalCombinations.TotalCount == 0 ? 0 : (float)(envimixValidationCount + defaultCarValidationCount) / totalCombinations.TotalCount,
            Players = players,
            Stars = stars,
            Combinations = combinations,
            EnvimixMostSkillpoints = envimixMostSkillpoints,
            EnvimixMostActivityPoints = envimixMostActivityPoints,
            EnvimixCompletion = envimixCompletion,
            DefaultCarMostSkillpoints = defaultCarMostSkillpoints,
            DefaultCarMostActivityPoints = defaultCarMostActivityPoints,
            DefaultCarCompletion = defaultCarCompletion,
            GlobalMostSkillpoints = globalMostSkillpoints,
            GlobalMostActivityPoints = globalMostActivityPoints,
            GlobalCompletion = globalCompletion
        });
    }
}
