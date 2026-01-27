using EnvimixWebAPI.Models;
using EnvimixWebAPI.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Caching.Hybrid;
using System.Diagnostics;
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
        IStarService starService,
        IEnvimaniaService envimaniaService,
        ITitleService titleService,
        IUserService userService,
        HttpContext context,
        HybridCache cache,
        ILogger<TitleService> logger,
        CancellationToken cancellationToken)
    {
        context.Response.Headers.ETag = $"\"{Guid.NewGuid():n}\"";

        var ratings = await ratingService.GetAveragesByTitleIdAsync(titleId, cancellationToken);
        var stars = await starService.GetStarsByTitleIdAsync(titleId, cancellationToken);
        var validations = await envimaniaService.GetValidationsByTitleIdAsync(titleId, cancellationToken);
        var playerRecords = await envimaniaService.GetPlayerRecordsByTitleIdAsync(titleId, cancellationToken);
        var totalCombinations = await envimaniaService.GetTotalCombinationsAsync(titleId, cancellationToken);
        var titleRelease = await titleService.GetTitleReleaseDateAsync(titleId, cancellationToken);
        var campaignReleases = await titleService.GetCampaignReleaseDatesAsync(titleId, cancellationToken);

        var calculationStartTimestamp = Stopwatch.GetTimestamp();

        var playerEnvimixSkillpoints = new Dictionary<string, int>();
        var playerEnvimixActivityPoints = new Dictionary<string, int>();
        var playerEnvimixCompleted = new Dictionary<string, int>();
        var playerDefaultCarSkillpoints = new Dictionary<string, int>();
        var playerDefaultCarActivityPoints = new Dictionary<string, int>();
        var playerDefaultCarCompleted = new Dictionary<string, int>();

        var envimixValidationCount = 0;
        var defaultCarValidationCount = 0;

        var mapCombinations = new Dictionary<string, Dictionary<string, CombinationStat>>();

        var combinationRecordCount = new Dictionary<string, CombinationRecordCount>();
        var playerEnvimixCombinationSkillpoints = new Dictionary<string, Dictionary<string, int>>();
        var playerEnvimixCombinationActivityPoints = new Dictionary<string, Dictionary<string, int>>();
        var playerEnvimixCombinationCompleted = new Dictionary<string, Dictionary<string, int>>();
        var playerDefaultCarCombinationSkillpoints = new Dictionary<string, Dictionary<string, int>>();
        var playerDefaultCarCombinationActivityPoints = new Dictionary<string, Dictionary<string, int>>();
        var playerDefaultCarCombinationCompleted = new Dictionary<string, Dictionary<string, int>>();

        foreach (var validation in validations)
        {
            var timeLoginPairs = playerRecords[$"{validation.MapId}_{validation.CarId}_{validation.Gravity}_{validation.Laps}"]
                .OrderBy(x => x.Time) // TODO: somehow put OrderBy as part of original query
                .ToArray();

            var skillpoints = timeLoginPairs
                .GroupBy(x => x.Time)
                .SelectMany(g => new[] { g.Key, g.Count() })
                .ToArray();

            var rating = ratings.GetValueOrDefault(validation.MapId)?
                .GetValueOrDefault($"{validation.CarId}_{validation.Gravity}_Time");

            var isDefaultCar = validation.IsDefaultCar();
            var isMainCampaign = validation.Map.Campaign?.Name == "";

            if (isMainCampaign)
            {
                if (isDefaultCar)
                {
                    defaultCarValidationCount++;
                }
                else
                {
                    envimixValidationCount++;
                }
            }

            if (!mapCombinations.TryGetValue(validation.MapId, out var combinations))
            {
                mapCombinations[validation.MapId] = combinations = [];
            }

            combinations[$"{validation.CarId}_{validation.Gravity}"] = new CombinationStat
            {
                ValidationLogin = isDefaultCar ? "" : validation.UserId,
                ValidationDrivenAt = isDefaultCar ? "" : validation.DrivenAt.ToUnixTimeSeconds().ToString(),
                Difficulty = rating?.Difficulty ?? -1,
                Quality = rating?.Quality ?? -1,
                Skillpoints = skillpoints
            };

            var totalRecordCount = timeLoginPairs.Length;

            var combinationKey = $"{validation.CarId}_{validation.Gravity}";
            if (!combinationRecordCount.TryGetValue(combinationKey, out var recCount))
            {
                combinationRecordCount[combinationKey] = recCount = new CombinationRecordCount();
            }

            if (isDefaultCar)
            {
                recCount.DefaultCar += totalRecordCount;
            }
            else
            {
                recCount.Envimix += totalRecordCount;
            }
            recCount.Global += totalRecordCount;

            if (!playerDefaultCarCombinationSkillpoints.TryGetValue(combinationKey, out var combSkillpoints))
            {
                playerDefaultCarCombinationSkillpoints[combinationKey] = combSkillpoints = [];
            }
            if (!playerDefaultCarCombinationActivityPoints.TryGetValue(combinationKey, out var combActivityPoints))
            {
                playerDefaultCarCombinationActivityPoints[combinationKey] = combActivityPoints = [];
            }
            if (!playerDefaultCarCombinationCompleted.TryGetValue(combinationKey, out var combCompleted))
            {
                playerDefaultCarCombinationCompleted[combinationKey] = combCompleted = [];
            }
            if (!playerEnvimixCombinationSkillpoints.TryGetValue(combinationKey, out var ecombSkillpoints))
            {
                playerEnvimixCombinationSkillpoints[combinationKey] = ecombSkillpoints = [];
            }
            if (!playerEnvimixCombinationActivityPoints.TryGetValue(combinationKey, out var ecombActivityPoints))
            {
                playerEnvimixCombinationActivityPoints[combinationKey] = ecombActivityPoints = [];
            }
            if (!playerEnvimixCombinationCompleted.TryGetValue(combinationKey, out var ecombCompleted))
            {
                playerEnvimixCombinationCompleted[combinationKey] = ecombCompleted = [];
            }

            var worstRanks = timeLoginPairs
                .Select((x, idx) => new { x.Time, Rank = idx + 1 })
                .GroupBy(x => x.Time)
                .ToDictionary(
                    g => g.Key,
                    g => g.Max(x => x.Rank)
                );

            var campaignName = validation.Map.Campaign?.Name;

            DateTimeOffset? campaignRelease;
            if (campaignName is not null && campaignReleases.TryGetValue(campaignName, out var campaignReleaseDateTime))
            {
                campaignRelease = campaignReleaseDateTime;
            }
            else
            {
                campaignRelease = titleRelease;
            }

            foreach (var (time, login) in timeLoginPairs)
            {
                var rank = worstRanks[time];

                var loginSkillpoints = (int)Math.Ceiling((totalRecordCount - rank) * 100f / rank);

                var wr = timeLoginPairs[0].Time;
                var wrPb = wr * 1f / time;
                var activityPoints = (int)Math.Round(1000 * Math.Exp(totalRecordCount * (wrPb - 1)));

                if (!isDefaultCar && validation.UserId == login && campaignRelease.HasValue)
                {
                    var validationTimestampInSeconds = validation.DrivenAt.ToUnixTimeSeconds();
                    var titlePackReleaseTimestampInSeconds = campaignRelease.Value.ToUnixTimeSeconds();
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

                    if (isMainCampaign)
                    {
                        if (!playerDefaultCarCompleted.ContainsKey(login))
                        {
                            playerDefaultCarCompleted[login] = 0;
                        }
                        playerDefaultCarCompleted[login] += 1;
                    }

                    if (!combSkillpoints.ContainsKey(login))
                    {
                        combSkillpoints[login] = 0;
                    }
                    combSkillpoints[login] += loginSkillpoints;

                    if (!combActivityPoints.ContainsKey(login))
                    {
                        combActivityPoints[login] = 0;
                    }
                    combActivityPoints[login] += activityPoints;

                    if (isMainCampaign)
                    {
                        if (!combCompleted.ContainsKey(login))
                        {
                            combCompleted[login] = 0;
                        }
                        combCompleted[login] += 1;
                    }
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

                    if (isMainCampaign)
                    {
                        if (!playerEnvimixCompleted.ContainsKey(login))
                        {
                            playerEnvimixCompleted[login] = 0;
                        }
                        playerEnvimixCompleted[login] += 1;
                    }

                    if (!ecombSkillpoints.ContainsKey(login))
                    {
                        ecombSkillpoints[login] = 0;
                    }
                    ecombSkillpoints[login] += loginSkillpoints;

                    if (!ecombActivityPoints.ContainsKey(login))
                    {
                        ecombActivityPoints[login] = 0;
                    }
                    ecombActivityPoints[login] += activityPoints;

                    if (isMainCampaign)
                    {
                        if (!ecombCompleted.ContainsKey(login))
                        {
                            ecombCompleted[login] = 0;
                        }
                        ecombCompleted[login] += 1;
                    }
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
                Score = totalCombinations.EnvimixCount == 0 ? 0 : (float)x.Value / totalCombinations.EnvimixCount
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
                Score = totalCombinations.DefaultCarCount == 0 ? 0 : (float)x.Value / totalCombinations.DefaultCarCount
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
                Score = totalCombinations.TotalCount == 0 ? 0 : (float)g.Sum(x => x.Value) / totalCombinations.TotalCount
            })
            .OrderByDescending(x => x.Score)
            .ToList();

        var envimixCombinationMostSkillpoints = playerEnvimixCombinationSkillpoints
            .ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value
                    .OrderByDescending(x => x.Value)
                    .ThenBy(x => x.Key)
                    .Select(x => new PlayerScore
                    {
                        Login = x.Key,
                        Score = x.Value
                    })
                    .ToList()
            );

        var envimixCombinationMostActivityPoints = playerEnvimixCombinationActivityPoints
            .ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value
                    .OrderByDescending(x => x.Value)
                    .ThenBy(x => x.Key)
                    .Select(x => new PlayerScore
                    {
                        Login = x.Key,
                        Score = x.Value
                    })
                    .ToList()
            );

        var envimixCombinationCompletion = playerEnvimixCombinationCompleted
            .ToDictionary(
                kvp => kvp.Key,
                kvp =>
                {
                    var count = totalCombinations.GetEnvimixCarCountForCombination(kvp.Key);
                    return kvp.Value
                        .OrderByDescending(x => x.Value)
                        .ThenBy(x => x.Key)
                        .Select(x => new PlayerCompletion
                        {
                            Login = x.Key,
                            Score = count == 0 ? 0 : (float)x.Value / count
                        })
                        .ToList();
                }
            );

        var defaultCarCombinationMostSkillpoints = playerDefaultCarCombinationSkillpoints
            .ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value
                    .OrderByDescending(x => x.Value)
                    .ThenBy(x => x.Key)
                    .Select(x => new PlayerScore
                    {
                        Login = x.Key,
                        Score = x.Value
                    })
                    .ToList()
            );

        var defaultCarCombinationMostActivityPoints = playerDefaultCarCombinationActivityPoints
            .ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value
                    .OrderByDescending(x => x.Value)
                    .ThenBy(x => x.Key)
                    .Select(x => new PlayerScore
                    {
                        Login = x.Key,
                        Score = x.Value
                    })
                    .ToList()
            );

        var defaultCarCombinationCompletion = playerDefaultCarCombinationCompleted
            .ToDictionary(
                kvp => kvp.Key,
                kvp =>
                {
                    var count = totalCombinations.GetDefaultCarCountForCombination(kvp.Key);
                    return kvp.Value
                        .OrderByDescending(x => x.Value)
                        .ThenBy(x => x.Key)
                        .Select(x => new PlayerCompletion
                        {
                            Login = x.Key,
                            Score = count == 0 ? 0 : (float)x.Value / count
                        })
                        .ToList();
                }
            );

        var globalCombinationMostSkillpoints = playerDefaultCarCombinationSkillpoints
            .Concat(playerEnvimixCombinationSkillpoints)
            .GroupBy(x => x.Key)
            .ToDictionary(
                g => g.Key,
                g => g.SelectMany(x => x.Value)
                    .GroupBy(x => x.Key)
                    .Select(gg => new PlayerScore
                    {
                        Login = gg.Key,
                        Score = gg.Sum(x => x.Value)
                    })
                    .OrderByDescending(x => x.Score)
                    .ToList()
            );

        var globalCombinationMostActivityPoints = playerDefaultCarCombinationActivityPoints
            .Concat(playerEnvimixCombinationActivityPoints)
            .GroupBy(x => x.Key)
            .ToDictionary(
                g => g.Key,
                g => g.SelectMany(x => x.Value)
                    .GroupBy(x => x.Key)
                    .Select(gg => new PlayerScore
                    {
                        Login = gg.Key,
                        Score = gg.Sum(x => x.Value)
                    })
                    .OrderByDescending(x => x.Score)
                    .ToList()
            );

        var globalCombinationCompletion = playerDefaultCarCombinationCompleted
            .Concat(playerEnvimixCombinationCompleted)
            .GroupBy(x => x.Key)
            .ToDictionary(
                g => g.Key,
                g => g.SelectMany(x => x.Value)
                    .GroupBy(x => x.Key)
                    .Select(gg => new PlayerCompletion
                    {
                        Login = gg.Key,
                        Score = totalCombinations.DefaultCarCount == 0 ? 0 : (float)gg.Sum(x => x.Value) / totalCombinations.DefaultCarCount
                    })
                    .OrderByDescending(x => x.Score)
                    .ToList()
            );

        var calculationElapsed = Stopwatch.GetElapsedTime(calculationStartTimestamp);
        logger.LogInformation("Title stats calculated in {ElapsedMilliseconds} ms", calculationElapsed.TotalMilliseconds);

        var players = await cache.GetOrCreateAsync($"TitleStatsPlayers_{titleId}", async entry =>
        {
            return await userService.GetTitleUserInfosAsync(globalCompletion.Select(x => x.Login), cancellationToken);
        }, new() { Expiration = TimeSpan.FromHours(1) }, tags: ["user"], cancellationToken: cancellationToken);

        return TypedResults.Ok(new TitleStats
        {
            EnvimixCompletionPercentage = totalCombinations.EnvimixCount == 0 ? 0 : (float)envimixValidationCount / totalCombinations.EnvimixCount,
            DefaultCarCompletionPercentage = totalCombinations.DefaultCarCount == 0 ? 0 : (float)defaultCarValidationCount / totalCombinations.DefaultCarCount,
            GlobalCompletionPercentage = totalCombinations.TotalCount == 0 ? 0 : (float)(envimixValidationCount + defaultCarValidationCount) / totalCombinations.TotalCount,
            Players = players,
            Stars = stars,
            Combinations = mapCombinations,
            EnvimixMostSkillpoints = envimixMostSkillpoints,
            EnvimixMostActivityPoints = envimixMostActivityPoints,
            EnvimixCompletion = envimixCompletion,
            DefaultCarMostSkillpoints = defaultCarMostSkillpoints,
            DefaultCarMostActivityPoints = defaultCarMostActivityPoints,
            DefaultCarCompletion = defaultCarCompletion,
            GlobalMostSkillpoints = globalMostSkillpoints,
            GlobalMostActivityPoints = globalMostActivityPoints,
            GlobalCompletion = globalCompletion,
            CombinationRecordCount = combinationRecordCount,
            EnvimixCombinationMostSkillpoints = envimixCombinationMostSkillpoints,
            EnvimixCombinationMostActivityPoints = envimixCombinationMostActivityPoints,
            EnvimixCombinationCompletion = envimixCombinationCompletion,
            DefaultCarCombinationMostSkillpoints = defaultCarCombinationMostSkillpoints,
            DefaultCarCombinationMostActivityPoints = defaultCarCombinationMostActivityPoints,
            DefaultCarCombinationCompletion = defaultCarCombinationCompletion,
            GlobalCombinationMostSkillpoints = globalCombinationMostSkillpoints,
            GlobalCombinationMostActivityPoints = globalCombinationMostActivityPoints,
            GlobalCombinationCompletion = globalCombinationCompletion
        });
    }
}
