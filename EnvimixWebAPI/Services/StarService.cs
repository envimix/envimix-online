using EnvimixWebAPI.Models;
using OneOf;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using EnvimixWebAPI.Entities;

namespace EnvimixWebAPI.Services;

public interface IStarService
{
    Task<OneOf<bool, ValidationFailureResponse>>
        StarAsync(RatingStarRequest request, ClaimsPrincipal principal, CancellationToken cancellationToken);

    Task<OneOf<bool, ValidationFailureResponse>>
        UnstarAsync(RatingStarRequest request, ClaimsPrincipal principal, CancellationToken cancellationToken);

    Task<bool> HasStarAsync(string mapUid, string car, int gravity, ClaimsPrincipal principal, CancellationToken cancellationToken);
}

public sealed class StarService(
    AppDbContext db,
    IModService modService,
    IUserService userService,
    IMapService mapService,
    ILogger<StarService> logger) : IStarService
{
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
        }

        logger.LogInformation("User {user} unstarred map {mapUid} with car {car} and gravity {gravity}.",
            principal.Identity.Name, request.MapUid, request.Filter.Car, request.Filter.Gravity);

        return true;
    }
}
