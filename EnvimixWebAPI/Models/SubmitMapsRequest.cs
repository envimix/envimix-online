namespace EnvimixWebAPI.Models;

public sealed class SubmitMapsRequest
{
    public required string TitleId { get; set; }
    public required MapInfo[] Maps { get; set; }
}
