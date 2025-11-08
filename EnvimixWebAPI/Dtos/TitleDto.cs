using System.Text.Json.Serialization;

namespace EnvimixWebAPI.Dtos;

public sealed class TitleDto
{
    [JsonPropertyName(nameof(Id))] public required string Id { get; set; }
    [JsonPropertyName(nameof(DisplayName))] public string? DisplayName { get; set; }
}
