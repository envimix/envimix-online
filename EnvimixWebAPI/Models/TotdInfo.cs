using System.Text.Json.Serialization;

namespace EnvimixWebAPI.Models;

public sealed class TotdInfo
{
    [JsonPropertyName(nameof(Map))] public required MapInfo Map { get; set; }
    [JsonPropertyName(nameof(NextAt))] public required string NextAt { get; set; }
}
