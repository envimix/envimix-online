using EnvimixWebAPI.Entities;
using EnvimixWebAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using OneOf;
using System.Diagnostics;
using System.Security.Claims;

namespace EnvimixWebAPI.Services;

public interface IStarService
{
    Task<OneOf<bool, ValidationFailureResponse>>
        StarAsync(RatingStarRequest request, ClaimsPrincipal principal, CancellationToken cancellationToken);

    Task<OneOf<bool, ValidationFailureResponse>>
        UnstarAsync(RatingStarRequest request, ClaimsPrincipal principal, CancellationToken cancellationToken);

    Task<bool> HasStarAsync(string mapUid, string car, int gravity, ClaimsPrincipal principal, CancellationToken cancellationToken);

    Task<Dictionary<string, Star>> GetStarsByMapUidAsync(string mapUid, CancellationToken cancellationToken);
    Task<Dictionary<string, Dictionary<string, Star>>> GetStarsByTitleIdAsync(string titleId, CancellationToken cancellationToken);
}

public sealed class StarService(
    AppDbContext db,
    IModService modService,
    IUserService userService,
    IMapService mapService,
    HybridCache cache,
    ILogger<StarService> logger) : IStarService
{
    private static readonly ActivitySource ActivitySource = new("EnvimixWebAPI.Services.StarService");

    public async Task<bool> HasStarAsync(string mapUid, string car, int gravity, ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        if (principal.Identity?.Name is null)
        {
            throw new Exception("ClaimsIdentity.Name is null");
        }

        return await db.Stars
            .Include(r => r.Map)
            .Include(r => r.Car)
            .AnyAsync(r => r.Map.Id == mapUid
                && r.Car.Id == car
                && r.Gravity == gravity, cancellationToken);
    }

    public async Task<OneOf<bool, ValidationFailureResponse>> StarAsync(RatingStarRequest request, ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        // VALIDATION START

        if (!Validator.ValidateMapUid(request.MapUid))
        {
            return new ValidationFailureResponse("Invalid MapUid");
        }

        if (!modService.IsValid(request.Filter))
        {
            return new ValidationFailureResponse("Invalid filter");
        }

        // VALIDATION END

        if (principal.Identity?.Name is null)
        {
            throw new Exception("ClaimsIdentity.Name is null");
        }

        var star = await db.Stars
            .Include(r => r.User)
            .Include(r => r.Map)
            .Include(r => r.Car)
            .FirstOrDefaultAsync(r => r.User.Id == principal.Identity.Name
                && r.Map.Id == request.MapUid
                && r.Car.Id == request.Filter.Car
                && r.Gravity == request.Filter.Gravity, cancellationToken);

        if (star is null)
        {
            star = new StarEntity
            {
                User = await userService.GetAsync(principal.Identity.Name, cancellationToken) ?? throw new Exception("User not found"),
                Map = await mapService.GetAsync(request.MapUid, cancellationToken) ?? throw new Exception("Map not found"),
                Car = await modService.GetOrAddCarAsync(request.Filter.Car, cancellationToken),
                Gravity = request.Filter.Gravity,
                CreatedAt = DateTimeOffset.UtcNow
            };

            await db.Stars.AddAsync(star, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);

            await cache.RemoveAsync($"StarsByTitleId_{star.Map.TitlePackId}", CancellationToken.None);
        }

        logger.LogInformation("User {user} starred map {mapUid} with car {car} and gravity {gravity}.",
            principal.Identity.Name, request.MapUid, request.Filter.Car, request.Filter.Gravity);

        return true;
    }

    public async Task<OneOf<bool, ValidationFailureResponse>> UnstarAsync(RatingStarRequest request, ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        // VALIDATION START

        if (!Validator.ValidateMapUid(request.MapUid))
        {
            return new ValidationFailureResponse("Invalid MapUid");
        }

        if (!modService.IsValid(request.Filter))
        {
            return new ValidationFailureResponse("Invalid filter");
        }

        // VALIDATION END

        if (principal.Identity?.Name is null)
        {
            throw new Exception("ClaimsIdentity.Name is null");
        }

        var star = await db.Stars
            .Include(r => r.User)
            .Include(r => r.Map)
            .Include(r => r.Car)
            .FirstOrDefaultAsync(r => r.User.Id == principal.Identity.Name
                && r.Map.Id == request.MapUid
                && r.Car.Id == request.Filter.Car
                && r.Gravity == request.Filter.Gravity, cancellationToken);

        if (star is not null)
        {
            db.Stars.Remove(star);
            await db.SaveChangesAsync(cancellationToken);

            await cache.RemoveAsync($"StarsByTitleId_{star.Map.TitlePackId}", CancellationToken.None);
        }

        logger.LogInformation("User {user} unstarred map {mapUid} with car {car} and gravity {gravity}.",
            principal.Identity.Name, request.MapUid, request.Filter.Car, request.Filter.Gravity);

        return true;
    }

    public async Task<Dictionary<string, Star>> GetStarsByMapUidAsync(string mapUid, CancellationToken cancellationToken)
    {
        return await db.Stars
            .Include(x => x.User)
            .Where(x => x.Map.Id == mapUid)
            .ToDictionaryAsync(x => $"{x.CarId}_{x.Gravity}_Time", x => new Star
            {
                Login = x.User.Id,
                Nickname = x.User.Nickname ?? "",
            }, cancellationToken);
    }

    public async Task<Dictionary<string, Dictionary<string, Star>>> GetStarsByTitleIdAsync(string titleId, CancellationToken cancellationToken)
    {
        using var activity = ActivitySource.StartActivity(nameof(GetStarsByTitleIdAsync));
        activity?.SetTag("titleId", titleId);

        return await cache.GetOrCreateAsync($"StarsByTitleId_{titleId}", async entry =>
        {
            var starsFromDb = await db.Stars
                .Include(x => x.User)
                .Where(x => x.Map.TitlePackId == titleId && x.Map.IsCampaignMap)
                .ToListAsync(entry);

            var starsByMap = new Dictionary<string, Dictionary<string, Star>>();

            foreach (var starGroup in starsFromDb.GroupBy(x => x.MapId))
            {
                var stars = new Dictionary<string, Star>();

                foreach (var star in starGroup)
                {
                    stars[$"{star.CarId}_{star.Gravity}_Time"] = new Star
                    {
                        Login = star.User.Id,
                        Nickname = star.User.Nickname ?? "",
                    };
                }

                starsByMap[starGroup.Key] = stars;
            }

            return starsByMap;
        }, new() { Expiration = TimeSpan.FromHours(1) }, cancellationToken: cancellationToken);
    }
}
