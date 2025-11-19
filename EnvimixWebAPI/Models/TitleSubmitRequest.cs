namespace EnvimixWebAPI.Models;

public sealed class TitleSubmitRequest
{
    public required string TitleId { get; init; }
    public required string Name { get; init; }
    public required string Version { get; init; }
}
