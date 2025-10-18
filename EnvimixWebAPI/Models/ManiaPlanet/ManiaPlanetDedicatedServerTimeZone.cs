namespace EnvimixWebAPI.Models.ManiaPlanet;

public sealed class ManiaPlanetDedicatedServerTimeZone
{
    public required string Name { get; set; }
    public required bool Transitions { get; set; }
    public required bool Location { get; set; }
}