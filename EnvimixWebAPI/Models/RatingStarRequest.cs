namespace EnvimixWebAPI.Models;

public sealed class RatingStarRequest
{
    public required string MapUid { get; set; }
    public required RatingFilter Rating { get; set; }
}
