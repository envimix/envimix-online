using System.Text.Json.Serialization;

namespace EnvimixWebAPI.Models;

public sealed class Campaign
{
    [JsonPropertyName(nameof(Name))] public required string Name { get; set; }
    [JsonPropertyName(nameof(ReleasedAt))] public required string ReleasedAt { get; set; }
}
