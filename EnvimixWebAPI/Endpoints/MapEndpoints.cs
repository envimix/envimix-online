using EnvimixWebAPI.Models;
using EnvimixWebAPI.Models.Envimania;
using EnvimixWebAPI.Options;
using EnvimixWebAPI.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace EnvimixWebAPI.Endpoints;

public static class MapEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        group.WithTags("Map");

        group.MapGet("{mapUid}", GetMap);
    }

    private static async Task<Results<Ok<MapInfoResponse>, NotFound>> GetMap(
        string mapUid,
        AppDbContext db,
        IOptionsSnapshot<EnvimaniaOptions> envimaniaOptions,
        IEnvimaniaService envimaniaService,
        IRatingService ratingService,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var map = await db.Maps
            .Include(x => x.TitlePack)
            .FirstOrDefaultAsync(x => x.Id == mapUid, cancellationToken: cancellationToken);

        if (map is null)
        {
            return TypedResults.NotFound();
        }

        var validations = await envimaniaService.GetValidationsAsync(mapUid, cancellationToken);

        var ratings = new List<FilteredRating>();

        foreach (var car in envimaniaOptions.Value.Car)
        {
            var filter = new RatingFilter()
            {
                Car = car
            };

            var rating = await ratingService.GetAverageAsync(map.Id, filter, cancellationToken);

            ratings.Add(new()
            {
                Filter = filter,
                Rating = rating with
                {
                    Difficulty = rating.Difficulty is null ? -1 : rating.Difficulty,
                    Quality = rating.Quality is null ? -1 : rating.Quality
                }
            });
        }

        var userRatings = new List<FilteredRating>();

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
            TitlePack = map.TitlePack is null ? null : new()
            {
                Id = map.TitlePack.Id,
                DisplayName = map.TitlePack.DisplayName ?? ""
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
                GhostUrl = "" // TODO: read from DB
            }),
            Stars = await ratingService.GetStarsByMapUidAsync(map.Id, cancellationToken)
        };

        return TypedResults.Ok(mapResponse);
    }
}
