using System.Text.Json.Serialization;

namespace EnvimixWebAPI.Models;

public sealed class TitleStats
{
    [JsonPropertyName(nameof(Ratings))] public required Dictionary<string, Dictionary<string, Rating>> Ratings { get; set; }
    [JsonPropertyName(nameof(Stars))] public required Dictionary<string, Dictionary<string, Star>> Stars { get; set; }
    [JsonPropertyName(nameof(Validations))] public required Dictionary<string, Dictionary<string, ValidationInfo>> Validations { get; set; }
    [JsonPropertyName(nameof(Skillpoints))] public required Dictionary<string, Dictionary<string, int[]>> Skillpoints { get; set; }
}
