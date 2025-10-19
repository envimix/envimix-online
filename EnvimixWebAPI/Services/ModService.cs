using EnvimixWebAPI.Entities;
using EnvimixWebAPI.Models;
using EnvimixWebAPI.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EnvimixWebAPI.Services;

public interface IModService
{
    Task<CarEntity> GetOrAddCarAsync(string carName, CancellationToken cancellationToken);
    bool IsValid(RatingFilter filter);
    bool IsValidCar(string carName);
    bool IsValidGravity(int gravity);
}

public sealed class ModService(AppDbContext db, IOptionsSnapshot<EnvimaniaOptions> envimaniaOptions) : IModService
{
    public bool IsValidCar(string carName)
    {
        return envimaniaOptions.Value.Car?.Contains(carName) == true;
    }

    public bool IsValidGravity(int gravity)
    {
        return envimaniaOptions.Value.Gravity?.Contains(gravity) == true;
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
