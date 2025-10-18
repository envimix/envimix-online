namespace EnvimixWebAPI.Models;

public sealed class RatingServerRequest
{
    public required UserInfo User { get; set; }
    public required string Car { get; set; }
    public required int Gravity { get; set; }
    public required Rating Rating { get; set; }
}
