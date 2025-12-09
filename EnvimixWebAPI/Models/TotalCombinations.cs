namespace EnvimixWebAPI.Models;

public sealed record TotalCombinations(int EnvimixCount, int DefaultCarCount)
{ 
    public int TotalCount => EnvimixCount + DefaultCarCount;
}
