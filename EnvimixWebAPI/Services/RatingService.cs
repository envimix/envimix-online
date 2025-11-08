using EnvimixWebAPI.Entities;
using EnvimixWebAPI.Models;
using EnvimixWebAPI.Security;
using Microsoft.EntityFrameworkCore;
using OneOf;
using System.Security.Claims;

namespace EnvimixWebAPI.Services;

public interface IRatingService
{
    Task<OneOf<RatingClientResponse, ValidationFailureResponse, ActionForbiddenResponse>>
        SetAsync(RatingClientRequest request, ClaimsPrincipal principal, CancellationToken cancellationToken);

    Task<OneOf<RatingServerResponse, ValidationFailureResponse, ActionForbiddenResponse>>
        SetAsync(RatingServerRequest[] request, ClaimsPrincipal principal, CancellationToken cancellationToken);

    Task<Rating> GetAverageAsync(string mapUid, RatingFilter filter, CancellationToken cancellationToken);
    Task<List<FilteredRating>> GetByUserLoginAsync(string mapUid, string login, CancellationToken cancellationToken);
    Task<Dictionary<string, List<FilteredRating>>> GetByUserLoginsAsync(string mapUid, IEnumerable<string> userLogins, CancellationToken cancellationToken);
}

public sealed class RatingService(
    AppDbContext db,
    IUserService userService,
    IMapService mapService,
    IModService modService,
    ILogger<RatingService> logger) : IRatingService
{
    private static ValidationFailureResponse? Validate(Rating rating)
    {
        var difficulty = rating.Difficulty;
        var quality = rating.Quality;

        if (difficulty.HasValue && difficulty.Value != -1 && (difficulty.Value < 0 || difficulty.Value > 1))
        {
            return new ValidationFailureResponse("Invalid difficulty");
        }

        if (quality.HasValue && quality.Value != -1 && (quality.Value < 0 || quality.Value > 1))
        {
            return new ValidationFailureResponse("Invalid quality");
        }

        return null;
    }

    public async Task<OneOf<RatingClientResponse, ValidationFailureResponse, ActionForbiddenResponse>> SetAsync(RatingClientRequest request, ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        // VALIDATION START

        if (!Validator.ValidateMapUid(request.Map.Uid))
        {
            return new ValidationFailureResponse("Invalid MapUid");
        }

        if (!modService.IsValidCar(request.Car))
        {
            return new ValidationFailureResponse("Invalid Car");
        }

        if (!modService.IsValidGravity(request.Gravity))
        {
            return new ValidationFailureResponse("Invalid Gravity");
        }

        var banReason = principal.FindFirstValue("BanReason");

        if (banReason is not null)
        {
            return ActionForbiddenResponse.UserLoginBanned;
        }

        if (Validate(request.Rating) is ValidationFailureResponse v)
        {
            return v;
        }

        // VALIDATION END

        if (principal.Identity?.Name is null)
        {
            throw new Exception("ClaimsIdentity.Name is null");
        }

        var rating = await db.Ratings
            .Include(r => r.User)
            .Include(r => r.Map)
            .Include(r => r.Car)
            .FirstOrDefaultAsync(r => r.User.Id == principal.Identity.Name
                && r.Map.Id == request.Map.Uid
                && r.Car.Id == request.Car
                && r.Gravity == request.Gravity
                && r.Server == null, cancellationToken);

        if (rating is null)
        {
            rating = new RatingEntity
            {
                User = await userService.GetAsync(principal.Identity.Name, cancellationToken) ?? throw new Exception("User not found"),
                Map = await mapService.GetAddOrUpdateAsync(request.Map, server: null, cancellationToken),
                Car = await modService.GetOrAddCarAsync(request.Car, cancellationToken),
                Gravity = request.Gravity,
                CreatedAt = DateTimeOffset.UtcNow
            };

            await db.Ratings.AddAsync(rating, cancellationToken);
        }

        rating.UpdatedAt = DateTimeOffset.UtcNow;
        rating.Difficulty = request.Rating.Difficulty;
        rating.Quality = request.Rating.Quality;

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("User {user} rated map {mapUid} with car {car} and gravity {gravity} with difficulty '{difficulty}' and quality '{quality}'.",
            principal.Identity.Name, request.Map.Uid, request.Car, request.Gravity, request.Rating.Difficulty, request.Rating.Quality);

        var avgRating = await GetAverageAsync(request.Map.Uid, new()
        {
            Car = request.Car,
            Gravity = request.Gravity
        }, cancellationToken);

        return new RatingClientResponse
        {
            Rating = avgRating
        };
    }

    public async Task<OneOf<RatingServerResponse, ValidationFailureResponse, ActionForbiddenResponse>> SetAsync(RatingServerRequest[] request, ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        // VALIDATION START

        foreach (var r in request)
        {
            if (!modService.IsValidCar(r.Car))
            {
                return new ValidationFailureResponse("Invalid Car");
            }

            if (!modService.IsValidGravity(r.Gravity))
            {
                return new ValidationFailureResponse("Invalid Gravity");
            }

            var banReason = principal.FindFirstValue("BanReason");

            if (banReason is not null)
            {
                return ActionForbiddenResponse.UserLoginBanned;
            }

            if (Validate(r.Rating) is ValidationFailureResponse v)
            {
                return v;
            }
        }

        // VALIDATION END

        var serverLogin = principal.Identity?.Name ?? throw new Exception("ClaimsIdentity.Name is null");
        var sessionGuid = Guid.Parse(principal.FindFirstValue(EnvimaniaClaimTypes.SessionGuid) ?? throw new Exception("Session GUID is null"));
        var mapUid = principal.FindFirstValue(EnvimaniaClaimTypes.SessionMapUid) ?? throw new Exception("Session MapUid is null");

        foreach (var req in request)
        {
            var rating = await db.Ratings
                .Include(r => r.User)
                .Include(r => r.Map)
                .Include(r => r.Car)
                .FirstOrDefaultAsync(r => r.User.Id == req.User.Login
                    && r.Map.Id == mapUid
                    && r.Car.Id == req.Car
                    && r.Gravity == req.Gravity
                    && r.Server!.Id == serverLogin, cancellationToken);

            if (rating is null)
            {
                var userInDb = await userService.GetAddOrUpdateAsync(req.User, cancellationToken);

                rating = new RatingEntity
                {
                    User = userInDb,
                    Map = await mapService.GetAsync(mapUid, cancellationToken) ?? throw new Exception($"Map not found in database ({mapUid})"),
                    Car = await modService.GetOrAddCarAsync(req.Car, cancellationToken),
                    Gravity = req.Gravity,
                    CreatedAt = DateTimeOffset.UtcNow,
                    ServerId = serverLogin
                };

                await db.Ratings.AddAsync(rating, cancellationToken);
            }

            var difficulty = req.Rating.Difficulty is -1 ? rating.Difficulty : req.Rating.Difficulty;
            var quality = req.Rating.Quality is -1 ? rating.Quality : req.Rating.Quality;

            rating.UpdatedAt = DateTimeOffset.UtcNow;
            rating.Difficulty = difficulty;
            rating.Quality = quality;

            logger.LogInformation("User {user} rated map {mapUid} with car {car} and gravity {gravity} with difficulty '{difficulty}' and quality '{quality}'.",
                req.User.Login, mapUid, req.Car, req.Gravity, difficulty, quality);
        }

        await db.SaveChangesAsync(cancellationToken);

        var ratings = new List<FilteredRating>();

        foreach (var req in request)
        {
            var filter = new RatingFilter()
            {
                Car = req.Car,
                Gravity = req.Gravity
            };

            var avgRating = await GetAverageAsync(mapUid, filter, cancellationToken);

            ratings.Add(new()
            {
                Filter = filter,
                Rating = avgRating with
                {
                    Difficulty = avgRating.Difficulty is null ? -1 : avgRating.Difficulty,
                    Quality = avgRating.Quality is null ? -1 : avgRating.Quality,
                }
            });
        }

        return new RatingServerResponse
        {
            Ratings = ratings
        };
    }

    public async Task<Rating> GetAverageAsync(string mapUid, RatingFilter filter, CancellationToken cancellationToken)
    {
        var avgDifficulty = await db.Ratings
            .Where(x => x.Map.Id == mapUid && x.Car.Id == filter.Car && x.Gravity == filter.Gravity && x.Difficulty != null)
            .GroupBy(x => x.User)
            .Select(x => x.OrderByDescending(x => x.CreatedAt).First())
            .ToListAsync(cancellationToken);

        var avgQuality = await db.Ratings
            .Where(x => x.Map.Id == mapUid && x.Car.Id == filter.Car && x.Gravity == filter.Gravity && x.Quality != null)
            .GroupBy(x => x.User)
            .Select(x => x.OrderByDescending(x => x.CreatedAt).First())
            .ToListAsync(cancellationToken);

        return new(avgDifficulty.Average(x => x.Difficulty), avgQuality.Average(x => x.Quality));
    }

    public async Task<List<FilteredRating>> GetByUserLoginAsync(string mapUid, string login, CancellationToken cancellationToken)
    {
        var ratingsFromDb = await db.Ratings
            .Include(x => x.User)
            .Where(x => x.Map.Id == mapUid && x.User.Id == login && (x.Difficulty != null || x.Quality != null))
            .GroupBy(x => new { x.CarId, x.Gravity })
            .Select(x => x.OrderByDescending(x => x.CreatedAt).First())
            .ToListAsync(cancellationToken);

        var ratings = ratingsFromDb.Select(rating => new FilteredRating()
        {
            Filter = new()
            {
                Car = rating.CarId,
                Gravity = rating.Gravity,
                Type = Models.Envimania.EnvimaniaLeaderboardType.Time
            },
            Rating = new(rating.Difficulty, rating.Quality)
        }).ToList();

        return ratings;
    }

    public async Task<Dictionary<string, List<FilteredRating>>> GetByUserLoginsAsync(string mapUid, IEnumerable<string> userLogins, CancellationToken cancellationToken)
    {
        var ratingsFromDb = await db.Ratings
            .Where(x => x.Map.Id == mapUid && userLogins.Contains(x.User.Id) && (x.Difficulty != null || x.Quality != null))
            .GroupBy(x => new { userId = x.User.Id, carId = x.Car.Id, x.Gravity })
            .Select(x => x.OrderByDescending(x => x.CreatedAt).First())
            .ToListAsync(cancellationToken);

        var ratings = new Dictionary<string, List<FilteredRating>>();

        foreach (var userRatings in ratingsFromDb.GroupBy(x => x.User.Id))
        {
            ratings[userRatings.Key] = userRatings.Select(rating => new FilteredRating()
            {
                Filter = new()
                {
                    Car = rating.CarId,
                    Gravity = rating.Gravity,
                    Type = Models.Envimania.EnvimaniaLeaderboardType.Time
                },
                Rating = new(rating.Difficulty, rating.Quality)
            }).ToList();
        }

        return ratings;
    }
}