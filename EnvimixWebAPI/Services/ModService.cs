using EnvimixWebAPI.Entities;
using EnvimixWebAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace EnvimixWebAPI.Services;

public interface IModService
{
    Task<CarEntity> GetOrAddCarAsync(string carName, CancellationToken cancellationToken);
    bool IsValid(RatingFilter filter);
    bool IsValidCar(string carName);
    bool IsValidGravity(int gravity);
}

public sealed class ModService(
    AppDbContext db,
    IConfiguration config,
    IMemoryCache memoryCache) : IModService
{
    public bool IsValidCar(string carName)
    {
        var cars = memoryCache.GetOrCreate(CacheHelper.GetCarsKey(), entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
            return config.GetSection("Envimania:Car").Get<HashSet<string>>();
        });

        return cars?.Contains(carName) == true;
    }

    public bool IsValidGravity(int gravity)
    {
        var allowedGravity = memoryCache.GetOrCreate(CacheHelper.GetGravityKey(), entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
            return config.GetSection("Envimania:Gravity").Get<HashSet<int>>();
        });

        return allowedGravity?.Contains(gravity) == true;
    }

    public async Task<CarEntity> GetOrAddCarAsync(string carName, CancellationToken cancellationToken)
    {
        var car = await db.Cars.FirstOrDefaultAsync(x => x.Id == carName, cancellationToken);

        if (car is null)
        {
            car = new CarEntity
            {
                Id = carName
            };

            await db.Cars.AddAsync(car, cancellationToken);
        }

        return car;
    }

    public bool IsValid(RatingFilter filter)
    {
        return IsValidCar(filter.Car) && IsValidGravity(filter.Gravity);
    }
}
