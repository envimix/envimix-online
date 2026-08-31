using EnvimixWebAPI.Entities;
using EnvimixWebAPI.Extensions;
using EnvimixWebAPI.Models;
using EnvimixWebAPI.Models.Envimania;
using EnvimixWebAPI.Options;
using EnvimixWebAPI.Services;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Json;
using TmEssentials;

namespace EnvimixWebAPI.Endpoints;

public static class MapEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        group.WithTags("Map");

        group.MapPost("", SubmitMaps).RequireAuthorization(Policies.SuperAdminPolicy);
        group.MapGet("{mapUid}", GetMap);
        group.MapGet("{mapUid}/records", GetRecords);
        group.MapGet("{mapUid}/download", DownloadMap);
        group.MapPost("{mapUid}", VisitMap).RequireAuthorization(Policies.ManiaPlanetUserPolicy);
    }

    private static async Task<Ok> SubmitMaps(
        SubmitMapsRequest request, 
        AppDbContext db,
        HybridCache cache,
        CancellationToken cancellationToken)
    {
        var mapUids = request.Maps.Select(x => x.Uid).ToHashSet();
        var maps = await db.Maps
            .Where(x => mapUids.Contains(x.Id) || (x.TitlePackId == request.TitleId && x.IsCampaignMap))
            .ToListAsync(cancellationToken);

        var campaigns = await db.Campaigns
            .Where(x => x.TitlePackId == request.TitleId)
            .ToDictionaryAsync(x => x.Name, cancellationToken);

        var campaignsFromRequest = request.Maps
            .Select(x => x.Campaign)
            .OfType<string>()
            .Distinct();

        foreach (var campaignName in campaignsFromRequest)
        {
            if (campaigns.ContainsKey(campaignName))
            {
                continue;
            }

            var campaign = new CampaignEntity
            {
                Name = campaignName,
                TitlePackId = request.TitleId
            };

            await db.Campaigns.AddAsync(campaign, cancellationToken);
            campaigns[campaignName] = campaign;
        }

        foreach (var mapInfo in request.Maps)
        {
            var map = maps.FirstOrDefault(x => x.Id == mapInfo.Uid);

            if (map is null)
            {
                map = new MapEntity
                {
                    Id = mapInfo.Uid,
                };
                await db.Maps.AddAsync(map, cancellationToken);
            }

            map.Name = mapInfo.Name;
            map.Collection = mapInfo.Collection ?? "";
            if (!string.IsNullOrWhiteSpace(mapInfo.AuthorLogin))
            {
                map.AuthorLogin = mapInfo.AuthorLogin;
            }
            if (!string.IsNullOrWhiteSpace(mapInfo.AuthorNickname))
            {
                map.AuthorNickname = mapInfo.AuthorNickname;
            }
            map.TitlePackId = request.TitleId;
            map.IsCampaignMap = true;
            map.Order = mapInfo.Order;
            map.Laps = mapInfo.Laps;
            map.Campaign = campaigns!.GetValueOrDefault(mapInfo.Campaign);
            map.AuthorTime = mapInfo.AuthorTime;
            map.GoldTime = mapInfo.GoldTime;
            map.SilverTime = mapInfo.SilverTime;
            map.BronzeTime = mapInfo.BronzeTime;
        }

        // unset campaign maps that are not in the submitted list
        foreach (var map in maps.Where(x => x.TitlePackId == request.TitleId && x.IsCampaignMap && !mapUids.Contains(x.Id)))
        {
            map.IsCampaignMap = false;
        }

        await db.SaveChangesAsync(cancellationToken);

        await cache.RemoveAsync($"Totd_{request.TitleId}", CancellationToken.None);
        await cache.RemoveAsync($"PossibleEnvimixCombinations_{request.TitleId}", CancellationToken.None);

        return TypedResults.Ok();
    }

    private static async Task<Results<Ok<MapRecordsPage>, NotFound>> GetRecords(
        string mapUid,
        string? car,
        int? page,
        int? pageSize,
        bool? showAll,
        bool? worldRecordHistory,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var mapInfo = await db.Maps
            .Where(x => x.Id == mapUid)
            .Select(x => new
            {
                x.Collection,
                x.AuthorLogin,
                x.AuthorNickname
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (mapInfo is null)
        {
            return TypedResults.NotFound();
        }

        var requestedPageSize = Math.Clamp(pageSize ?? 50, 10, 100);
        var includeAllRecords = showAll == true;
        var includeWorldRecordHistory = worldRecordHistory == true;

        var mapRecordsQuery = db.Records.Where(record => record.MapId == mapUid);
        var worldRecordHistoryQuery = mapRecordsQuery
            .Where(record => !record.Removed && !db.Records.Any(other =>
                other.MapId == record.MapId
                && other.CarId == record.CarId
                && other.Gravity == record.Gravity
                && other.Laps == record.Laps
                && !other.Removed
                && other.Time <= record.Time
                && (other.DrivenAt < record.DrivenAt
                    || other.DrivenAt == record.DrivenAt && other.Id < record.Id)));

        var carStatsQuery = includeWorldRecordHistory ? worldRecordHistoryQuery : mapRecordsQuery;
        var carStats = await carStatsQuery
            .GroupBy(x => new { x.CarId, x.Car.Order })
            .Select(group => new
            {
                Id = group.Key.CarId,
                Order = group.Key.Order,
                RecordCount = includeAllRecords || includeWorldRecordHistory
                    ? group.Count()
                    : group.Select(record => record.UserId).Distinct().Count()
            })
            .OrderBy(x => x.Order == null)
            .ThenBy(x => x.Order)
            .ThenBy(x => x.Id)
            .ToArrayAsync(cancellationToken);

        var validators = await db.Records
            .Where(record => record.MapId == mapUid
                && record.Map.Laps > 0
                && record.Laps == record.Map.Laps
                && record.Gravity == 0
                && !record.Removed
                && !db.Records.Any(other =>
                    other.MapId == record.MapId
                    && other.CarId == record.CarId
                    && other.Laps == record.Laps
                    && other.Gravity == record.Gravity
                    && !other.Removed
                    && (other.DrivenAt < record.DrivenAt
                        || other.DrivenAt == record.DrivenAt && other.Id < record.Id)))
            .Select(record => new
            {
                record.CarId,
                Login = record.UserId,
                record.User.Nickname
            })
            .ToDictionaryAsync(x => x.CarId, cancellationToken);

        var cars = carStats
            .Select(carInfo =>
            {
                validators.TryGetValue(carInfo.Id, out var validator);
                var isDefaultCar = mapInfo.Collection switch
                {
                    "Canyon" => carInfo.Id == "CanyonCar",
                    "Stadium" => carInfo.Id == "StadiumCar",
                    "Valley" => carInfo.Id == "ValleyCar",
                    "Lagoon" => carInfo.Id == "LagoonCar",
                    _ => false
                };

                return new MapRecordCarInfo(
                    carInfo.Id,
                    carInfo.RecordCount,
                    isDefaultCar ? mapInfo.AuthorLogin ?? validator?.Login : validator?.Login,
                    isDefaultCar ? mapInfo.AuthorNickname ?? validator?.Nickname : validator?.Nickname);
            })
            .ToArray();

        car = string.IsNullOrWhiteSpace(car) || !cars.Any(x => x.Id == car)
            ? cars.FirstOrDefault()?.Id
            : car;
        var totalCount = cars.FirstOrDefault(x => x.Id == car)?.RecordCount ?? 0;
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)requestedPageSize));
        var requestedPage = Math.Clamp(page ?? 1, 1, totalPages);

        var allRecordsQuery = mapRecordsQuery.Where(x => x.CarId == car);

        var bestRecordsQuery = allRecordsQuery
            .Where(record => !db.Records.Any(other =>
                other.MapId == record.MapId &&
                other.CarId == record.CarId &&
                other.UserId == record.UserId &&
                (other.Time < record.Time ||
                    (other.Time == record.Time && other.DrivenAt > record.DrivenAt) ||
                    (other.Time == record.Time && other.DrivenAt == record.DrivenAt && other.Id > record.Id))));

        var recordsQuery = includeWorldRecordHistory
            ? worldRecordHistoryQuery.Where(record => record.CarId == car)
            : includeAllRecords ? allRecordsQuery : bestRecordsQuery;

        var orderedRecords = recordsQuery
            .OrderBy(x => x.Time)
            .ThenByDescending(x => x.DrivenAt)
            .ThenByDescending(x => x.Id);

        var projectedRecords = car is null
            ? []
            : await RecordEndpoints.ProjectWithId(orderedRecords
                .Skip((requestedPage - 1) * requestedPageSize)
                .Take(requestedPageSize))
                .ToArrayAsync(cancellationToken);

        RecordInfo[] records;

        if (includeWorldRecordHistory)
        {
            records = projectedRecords
                .Select((record, index) => record.ToRecordInfo(
                    totalCount - (requestedPage - 1) * requestedPageSize - index))
                .ToArray();
        }
        else if (includeAllRecords && projectedRecords.Length > 0)
        {
            var rankedRecordIds = await bestRecordsQuery
                .OrderBy(x => x.Time)
                .ThenByDescending(x => x.DrivenAt)
                .ThenByDescending(x => x.Id)
                .Select(x => x.Id)
                .ToArrayAsync(cancellationToken);
            var ranks = rankedRecordIds
                .Select((recordId, index) => (recordId, Rank: index + 1))
                .ToDictionary(x => x.recordId, x => x.Rank);

            records = projectedRecords
                .Select(record => record.ToRecordInfo(ranks.GetValueOrDefault(record.Id)))
                .ToArray();
        }
        else
        {
            records = projectedRecords
                .Select((record, index) => record.ToRecordInfo(
                    (requestedPage - 1) * requestedPageSize + index + 1))
                .ToArray();
        }

        return TypedResults.Ok(new MapRecordsPage(cars, car, requestedPage, requestedPageSize, totalCount, includeAllRecords, includeWorldRecordHistory, records));
    }

    private static async Task<Results<Ok<MapInfoResponse>, BadRequest<ValidationFailureResponse>, NotFound, ForbidHttpResult>> GetMap(
        string mapUid,
        AppDbContext db,
        IOptionsSnapshot<EnvimaniaOptions> envimaniaOptions,
        IEnvimaniaService envimaniaService,
        IRatingService ratingService,
        IStarService starService,
        IConfiguration configuration,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var map = await db.Maps
            .Include(x => x.TitlePack)
            .Include(x => x.Campaign)
            .FirstOrDefaultAsync(x => x.Id == mapUid, cancellationToken: cancellationToken);

        if (map is null)
        {
            return TypedResults.NotFound();
        }

        if (map.TitlePack?.ReleasedAt is not null && map.TitlePack.ReleasedAt > DateTimeOffset.UtcNow && !principal.IsInRole(Roles.Admin))
        {
            return TypedResults.Forbid();
        }

        var mapResponse = await GetMapInfoAsync(mapUid, envimaniaService, ratingService, starService, configuration, principal, map, cancellationToken);
        return TypedResults.Ok(mapResponse);
    }

    private static async Task<Results<FileContentHttpResult, NotFound>> DownloadMap(
        string mapUid,
        IMapService mapService,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var map = await mapService.GetWithDownloadAsync(mapUid, cancellationToken);

        if (map?.Data is null)
        {
            return TypedResults.NotFound();
        }

        // CORS middleware is ???
        if (context.Request.Headers.ContainsKey(CorsConstants.Origin))
        {
            context.Response.Headers.AccessControlAllowOrigin = "https://3d.gbx.tools";
            context.Response.Headers.AccessControlAllowMethods = "GET, OPTIONS";
            context.Response.Headers.AccessControlAllowHeaders = "*";
        }

        return TypedResults.File(map.Data.Data, "application/gbx", $"{TextFormatter.Deformat(map.Name)}.Map.Gbx", lastModified: map.Data.LastModifiedAt);
    }

    private static async Task<Results<Ok<MapInfoResponse>, BadRequest<ValidationFailureResponse>, NotFound, ForbidHttpResult>> VisitMap(
        string mapUid,
        AppDbContext db,
        IEnvimaniaService envimaniaService,
        IRatingService ratingService,
        IStarService starService,
        IMapService mapService,
        IUserService userService,
        IConfiguration configuration,
        ClaimsPrincipal principal,
        HttpRequest request,
        CancellationToken cancellationToken)
    {
        MapInfo? mapInfo = null;
        if (request.HttpContext.Features.Get<IHttpRequestBodyDetectionFeature>()?.CanHaveBody == true)
        {
            if (!request.HasJsonContentType())
            {
                return TypedResults.BadRequest(new ValidationFailureResponse("Map body must be JSON"));
            }

            try
            {
                mapInfo = await request.ReadFromJsonAsync(
                    AppJsonSerializerContext.Default.MapInfo,
                    cancellationToken);
            }
            catch (JsonException)
            {
                return TypedResults.BadRequest(new ValidationFailureResponse("Invalid map JSON"));
            }
        }

        if (mapInfo is not null && mapUid != mapInfo.Uid)
        {
            return TypedResults.BadRequest(new ValidationFailureResponse("Map UID does not match route"));
        }

        var userModel = await userService.GetAsync(principal.GetName(), cancellationToken);

        if (userModel is null)
        {
            return TypedResults.BadRequest(new ValidationFailureResponse("User not found"));
        }

        if (userModel.BanReason is not null)
        {
            return TypedResults.Forbid();
        }

        MapEntity map;

        if (mapInfo is not null)
        {
            map = await mapService.GetAddOrUpdateAsync(mapInfo, server: null, cancellationToken);
        }
        else
        {
            map = await db.Maps
                .Include(x => x.TitlePack)
                .Include(x => x.Campaign)
                .FirstOrDefaultAsync(x => x.Id == mapUid, cancellationToken)
                ?? new MapEntity { Id = mapUid };

            if (db.Entry(map).State == EntityState.Detached)
            {
                await db.Maps.AddAsync(map, cancellationToken);
            }
        }

        if (map.Campaign?.ReleasedAt is not null && map.Campaign.ReleasedAt > DateTimeOffset.UtcNow && !principal.IsInRole(Roles.Admin))
        {
            userModel.BanReason = "AUTOMATED: Attempted to access an unreleased campaign map";
            await db.SaveChangesAsync(CancellationToken.None);
            return TypedResults.Forbid();
        }

        if (map.TitlePack?.ReleasedAt is not null && map.TitlePack.ReleasedAt > DateTimeOffset.UtcNow && !principal.IsInRole(Roles.Admin))
        {
            userModel.BanReason = "AUTOMATED: Attempted to access an unreleased title pack map";
            await db.SaveChangesAsync(CancellationToken.None);

            return TypedResults.Forbid();
        }

        await db.MapVisits.AddAsync(new MapVisitEntity
        {
            Map = map,
            User = userModel,
            VisitedAt = DateTimeOffset.UtcNow
        }, cancellationToken);

        var mapResponse = await GetMapInfoAsync(mapUid, envimaniaService, ratingService, starService, configuration, principal, map, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(mapResponse);
    }

    private static async Task<MapInfoResponse> GetMapInfoAsync(
        string mapUid, 
        IEnvimaniaService envimaniaService, 
        IRatingService ratingService, 
        IStarService starService, 
        IConfiguration configuration,
        ClaimsPrincipal principal, 
        MapEntity map, 
        CancellationToken cancellationToken)
    {
        var validations = await envimaniaService.GetValidationsByMapUidAsync(mapUid, cancellationToken);

        var ratings = await ratingService.GetAveragesByMapUidAsync(mapUid, cancellationToken);

        var userRatings = new List<FilteredRating>();
        var medalInfo = configuration.GetSection("Medals").Get<Dictionary<string, MedalInfo>>()?.GetValueOrDefault(mapUid);

        if (principal.Identity?.IsAuthenticated == true && principal.Identity.Name is not null)
        {
            userRatings = await ratingService.GetByUserLoginAsync(map.Id, principal.Identity.Name, cancellationToken);

            foreach (var rating in userRatings)
            {
                rating.Rating = rating.Rating with
                {
                    Difficulty = rating.Rating.Difficulty is null ? -1 : rating.Rating.Difficulty,
                    Quality = rating.Rating.Quality is null ? -1 : rating.Rating.Quality
                };
            }
        }

        var mapResponse = new MapInfoResponse
        {
            Name = map.Name,
            Uid = map.Id,
            Collection = map.Collection,
            AuthorLogin = map.AuthorLogin,
            AuthorNickname = map.AuthorNickname,
            Laps = map.Laps,
            AuthorTime = map.AuthorTime,
            GoldTime = map.GoldTime,
            SilverTime = map.SilverTime,
            BronzeTime = map.BronzeTime,
            DuckTime = medalInfo?.Duck,
            STMTime = medalInfo?.STM,
            TitlePack = map.TitlePack is null ? null : new()
            {
                Id = map.TitlePack.Id,
                DisplayName = map.TitlePack.DisplayName ?? "",
                ReleasedAt = map.TitlePack.ReleasedAt?.ToUnixTimeSeconds().ToString() ?? "",
            },
            Campaign = map.Campaign is null ? null : new()
            {
                Name = map.Campaign.Name,
                ReleasedAt = map.Campaign.ReleasedAt?.ToUnixTimeSeconds().ToString() ?? "",
            },
            Ratings = ratings,
            UserRatings = userRatings,
            Validations = validations.ToDictionary(x => $"{x.Car.Id}_{x.Gravity}_{x.Laps}", rec => new EnvimaniaRecordInfo
            {
                User = new UserInfo
                {
                    Login = rec.User.Id,
                    Nickname = rec.User.Nickname ?? "",
                    Zone = rec.User.Zone?.Name ?? "",
                    AvatarUrl = rec.User.AvatarUrl ?? "",
                    Language = rec.User.Language ?? "",
                    Description = rec.User.Language ?? "",
                    Color = rec.User.Color ?? [-1, -1, -1],
                    SteamUserId = rec.User.SteamUserId ?? "",
                    FameStars = rec.User.FameStars ?? 0,
                    LadderPoints = rec.User.LadderPoints ?? 0,
                },
                Time = rec.Checkpoints.Last().Time,
                Score = rec.Checkpoints.Last().Score,
                NbRespawns = rec.Checkpoints.Last().NbRespawns,
                Distance = rec.Checkpoints.Last().Distance,
                Speed = rec.Checkpoints.Last().Speed,
                Verified = true,
                Projected = false,
                GhostUrl = "", // TODO: read from DB
                DrivenAt = rec.DrivenAt.ToUnixTimeSeconds().ToString(),
                Removed = rec.Removed,
            }),
            Stars = await starService.GetStarsByMapUidAsync(map.Id, cancellationToken),
            Skillpoints = await envimaniaService.GetSkillpointsByMapUidAsync(mapUid, cancellationToken)
        };

        return mapResponse;
    }
}
