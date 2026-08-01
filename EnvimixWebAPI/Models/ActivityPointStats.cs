using System.Text.Json.Serialization;

namespace EnvimixWebAPI.Models;

public sealed class ActivityPointStats
{
    [JsonPropertyName(nameof(Players))] public required Dictionary<string, TitleUserInfo> Players { get; set; }
    [JsonPropertyName(nameof(EnvimixMostActivityPoints))] public required List<PlayerScore> EnvimixMostActivityPoints { get; set; }
    [JsonPropertyName(nameof(DefaultCarMostActivityPoints))] public required List<PlayerScore> DefaultCarMostActivityPoints { get; set; }
    [JsonPropertyName(nameof(GlobalMostActivityPoints))] public required List<PlayerScore> GlobalMostActivityPoints { get; set; }
    [JsonPropertyName(nameof(EnvimixCombinationMostActivityPoints))] public required Dictionary<string, List<PlayerScore>> EnvimixCombinationMostActivityPoints { get; set; }
    [JsonPropertyName(nameof(DefaultCarCombinationMostActivityPoints))] public required Dictionary<string, List<PlayerScore>> DefaultCarCombinationMostActivityPoints { get; set; }
    [JsonPropertyName(nameof(GlobalCombinationMostActivityPoints))] public required Dictionary<string, List<PlayerScore>> GlobalCombinationMostActivityPoints { get; set; }
}
