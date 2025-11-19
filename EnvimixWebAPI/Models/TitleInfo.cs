using System.Text.Json.Serialization;

namespace EnvimixWebAPI.Models;

public sealed class TitleInfo
{
    [JsonPropertyName(nameof(Id))] public required string Id { get; set; }
    [JsonPropertyName(nameof(DisplayName))] public string? DisplayName { get; set; }
}
