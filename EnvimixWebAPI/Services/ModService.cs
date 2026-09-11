using EnvimixWebAPI.Entities;
using EnvimixWebAPI.Models;
using EnvimixWebAPI.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EnvimixWebAPI.Services;

public interface IModService
{
    Task<CarEntity> GetOrAddCarAsync(string carName, CancellationToken cancellationToken);
    string? GetCarIdFromPlayerModel(string? playerModelId);
    bool IsValid(RatingFilter filter);
    bool IsValidCar(string carName);
    bool IsValidGravity(int gravity);
}

public sealed class ModService(AppDbContext db, IOptionsSnapshot<EnvimaniaOptions> envimaniaOptions) : IModService
{
    public string? GetCarIdFromPlayerModel(string? playerModelId)
        => playerModelId switch
        {
            "CanyonCar" or "Vehicles\\CanyonCar.Item.Gbx" or "Vehicles\\CanyonCarTurbo.Item.Gbx" or "CanyonCar.Item.Gbx" => "CanyonCar",
            "StadiumCar" or "Vehicles\\StadiumCar.Item.Gbx" or "Vehicles\\StadiumCarTurbo.Item.Gbx" or "StadiumCar.Item.Gbx" => "StadiumCar",
            "ValleyCar" or "Vehicles\\ValleyCar.Item.Gbx" or "Vehicles\\ValleyCarTurbo.Item.Gbx" or "ValleyCar.Item.Gbx" => "ValleyCar",
            "LagoonCar" or "Vehicles\\LagoonCar.Item.Gbx" or "Vehicles\\LagoonCarTurbo.Item.Gbx" or "LagoonCar.Item.Gbx" => "LagoonCar",
            "TrafficCar" or "Vehicles\\TrafficCar.Item.Gbx" or "TrafficCar.Item.Gbx" => "TrafficCar",
            "DesertCar" or "Vehicles\\DesertCar.Item.Gbx" or "DesertCar.Item.Gbx" => "DesertCar",
            "RallyCar" or "Vehicles\\RallyCar.Item.Gbx" or "RallyCar.Item.Gbx" => "RallyCar",
            "SnowCar" or "Vehicles\\SnowCar.Item.Gbx" or "SnowCar.Item.Gbx" => "SnowCar",
            "IslandCar" or "Vehicles\\IslandCar.Item.Gbx" or "IslandCar.Item.Gbx" => "IslandCar",
            "BayCar" or "Vehicles\\BayCar.Item.Gbx" or "BayCar.Item.Gbx" => "BayCar",
            "CoastCar" or "Vehicles\\CoastCar.Item.Gbx" or "CoastCar.Item.Gbx" => "CoastCar",
            _ => null
        };

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
                Id = carName,
                Order = CarOrder.GetOrder(carName)
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
