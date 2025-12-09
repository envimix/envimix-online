using System.Text.Json.Serialization;

namespace EnvimixWebAPI.Models;

public sealed class CombinationStat
{
    [JsonPropertyName("VL")] public required string ValidationLogin { get; init; }
    [JsonPropertyName("VD")] public required string ValidationDrivenAt { get; init; }
    [JsonPropertyName("D")] public required float Difficulty { get; init; }
    [JsonPropertyName("Q")] public required float Quality { get; init; }
    [JsonPropertyName("S")] public required int[] Skillpoints { get; init; }
}
