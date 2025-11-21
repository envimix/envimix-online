using System.Text.Json.Serialization;

namespace EnvimixWebAPI.Models;

public sealed class MapInfo
{
    [JsonPropertyName(nameof(Name))] public required string Name { get; set; }
    [JsonPropertyName(nameof(Uid))] public required string Uid { get; set; }
    [JsonPropertyName(nameof(Order))] public int? Order { get; set; }
}
