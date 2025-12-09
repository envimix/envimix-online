using System.Text.Json.Serialization;

namespace EnvimixWebAPI.Models;

public sealed class TitleStats
{
    [JsonPropertyName(nameof(EnvimixCompletionPercentage))] public required float EnvimixCompletionPercentage { get; set; }
    [JsonPropertyName(nameof(DefaultCarCompletionPercentage))] public required float DefaultCarCompletionPercentage { get; set; }
    [JsonPropertyName(nameof(GlobalCompletionPercentage))] public required float GlobalCompletionPercentage { get; set; }
    [JsonPropertyName(nameof(Players))] public required Dictionary<string, TitleUserInfo> Players { get; set; }
    [JsonPropertyName(nameof(Combinations))] public required Dictionary<string, CombinationStat> Combinations { get; set; }
    [JsonPropertyName(nameof(Stars))] public required Dictionary<string, Dictionary<string, Star>> Stars { get; set; }
    [JsonPropertyName(nameof(EnvimixMostSkillpoints))] public required List<PlayerScore> EnvimixMostSkillpoints { get; set; }
    [JsonPropertyName(nameof(EnvimixMostActivityPoints))] public required List<PlayerScore> EnvimixMostActivityPoints { get; set; }
    [JsonPropertyName(nameof(EnvimixCompletion))] public required List<PlayerCompletion> EnvimixCompletion { get; set; }
    [JsonPropertyName(nameof(DefaultCarMostSkillpoints))] public required List<PlayerScore> DefaultCarMostSkillpoints { get; set; }
    [JsonPropertyName(nameof(DefaultCarMostActivityPoints))] public required List<PlayerScore> DefaultCarMostActivityPoints { get; set; }
    [JsonPropertyName(nameof(DefaultCarCompletion))] public required List<PlayerCompletion> DefaultCarCompletion { get; set; }
    [JsonPropertyName(nameof(GlobalMostSkillpoints))] public required List<PlayerScore> GlobalMostSkillpoints { get; set; }
    [JsonPropertyName(nameof(GlobalMostActivityPoints))] public required List<PlayerScore> GlobalMostActivityPoints { get; set; }
    [JsonPropertyName(nameof(GlobalCompletion))] public required List<PlayerCompletion> GlobalCompletion { get; set; }
}
