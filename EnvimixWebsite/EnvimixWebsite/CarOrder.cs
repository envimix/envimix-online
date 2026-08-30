namespace EnvimixWebsite;

public static class CarOrder
{
    private static readonly string[] Cars =
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
}
