using System.Text.Json.Serialization;

namespace EnvimixWebAPI.Models.ManiaPlanet;

public sealed class ManiaPlanetDedicatedServer
{
    public required string Login { get; set; }

    [JsonPropertyName("last_used_date")]
    public required ManiaPlanetDedicatedServerLastUsedDate LastUsedDate { get; set; }
}
