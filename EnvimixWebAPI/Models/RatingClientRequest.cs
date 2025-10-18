namespace EnvimixWebAPI.Models;

public sealed class RatingClientRequest
{
    public required MapInfo Map { get; set; }
    public required string Car { get; set; }
    public required int Gravity { get; set; }
    public required Rating Rating { get; set; }
}
