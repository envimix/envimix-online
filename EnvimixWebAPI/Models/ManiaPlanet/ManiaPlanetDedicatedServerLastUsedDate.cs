namespace EnvimixWebAPI.Models.ManiaPlanet;

public sealed class ManiaPlanetDedicatedServerLastUsedDate
{
    public required ManiaPlanetDedicatedServerTimeZone Timezone { get; set; }
    public required int Offset { get; set; }
    public required long Timestamp { get; set; }
}
