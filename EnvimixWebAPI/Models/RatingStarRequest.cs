namespace EnvimixWebAPI.Models;

public sealed class RatingStarRequest
{
    public required string MapUid { get; set; }
    public required RatingFilter Filter { get; set; }
}
