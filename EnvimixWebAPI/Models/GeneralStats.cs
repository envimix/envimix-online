using System.Text.Json.Serialization;

namespace EnvimixWebAPI.Models;

public sealed class GeneralStats
{
    [JsonPropertyName(nameof(Combinations))] public required Dictionary<string, Dictionary<string, CombinationStat>> Combinations { get; set; }
    [JsonPropertyName(nameof(Stars))] public required Dictionary<string, Dictionary<string, Star>> Stars { get; set; }
}
