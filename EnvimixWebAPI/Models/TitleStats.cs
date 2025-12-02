using System.Text.Json.Serialization;

namespace EnvimixWebAPI.Models;

public sealed class TitleStats
{
    [JsonPropertyName(nameof(Ratings))] public required Dictionary<string, Dictionary<string, Rating>> Ratings { get; set; }
    [JsonPropertyName(nameof(Stars))] public required Dictionary<string, Dictionary<string, Star>> Stars { get; set; }
    [JsonPropertyName(nameof(Validations))] public required Dictionary<string, Dictionary<string, ValidationInfo>> Validations { get; set; }
    [JsonPropertyName(nameof(Skillpoints))] public required Dictionary<string, Dictionary<string, int[]>> Skillpoints { get; set; }
    [JsonPropertyName(nameof(EnvimixOverallCompletion))] public required float EnvimixOverallCompletion { get; set; }
    [JsonPropertyName(nameof(EnvimixCompletion))] public required List<PlayerCompletion> EnvimixCompletion { get; set; }
    [JsonPropertyName(nameof(EnvimixMostSkillpoints))] public required List<PlayerScore> EnvimixMostSkillpoints { get; set; }
    [JsonPropertyName(nameof(EnvimixMostActivityPoints))] public required List<PlayerScore> EnvimixMostActivityPoints { get; set; }
}
