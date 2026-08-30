namespace EnvimixWebAPI;

public static class CarOrder
{
    public static string[] Cars { get; } =
    [
        "CanyonCar",
        "StadiumCar",
        "ValleyCar",
        "LagoonCar",
        "TrafficCar",
        "DesertCar",
        "SnowCar",
        "RallyCar",
        "IslandCar",
        "BayCar",
        "CoastCar"
    ];

    public static int GetIndex(string carId)
    {
        var index = Array.IndexOf(Cars, carId);
        return index < 0 ? int.MaxValue : index;
    }

    public static int? GetOrder(string carId)
    {
        var index = Array.IndexOf(Cars, carId);
        return index < 0 ? null : index;
    }
}
