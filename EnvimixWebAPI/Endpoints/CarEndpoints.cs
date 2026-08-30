using EnvimixWebAPI.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace EnvimixWebAPI.Endpoints;

public static class CarEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        group.WithTags("Car");
        group.MapGet("{carId}", GetCar);
    }

    private static async Task<Results<Ok<CarInfo>, NotFound>> GetCar(
        string carId,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var carExists = await db.Cars.AnyAsync(x => x.Id == carId, cancellationToken);
        if (!carExists)
        {
            return TypedResults.NotFound();
        }

        var recordsQuery = db.Records.Where(x => x.CarId == carId && !x.Removed);
        var recordCount = await recordsQuery.CountAsync(cancellationToken);
        var playerCount = await recordsQuery.Select(x => x.UserId).Distinct().CountAsync(cancellationToken);
        var records = await RecordEndpoints.GetRecent(recordsQuery, cancellationToken);

        return TypedResults.Ok(new CarInfo(carId, recordCount, playerCount, records));
    }
}
