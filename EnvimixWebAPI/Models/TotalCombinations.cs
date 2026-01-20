namespace EnvimixWebAPI.Models;

public sealed record TotalCombinations(int EnvimixCount, int DefaultCarCount, Dictionary<string, int> EnvironmentEnvimixMapCount)
{ 
    public int TotalCount => EnvimixCount + DefaultCarCount;

    public int GetTotalCountForCombination(string car)
    {
        var environment = car switch
        {
            "CanyonCar_0" => "Canyon",
            "StadiumCar_0" => "Stadium",
            "ValleyCar_0" => "Valley",
            "LagoonCar_0" => "Lagoon",
            _ => null
        };

        if (environment is not null && EnvironmentEnvimixMapCount.TryGetValue(environment, out var count))
        {
            return count;
        }

        return DefaultCarCount;
    }
}
