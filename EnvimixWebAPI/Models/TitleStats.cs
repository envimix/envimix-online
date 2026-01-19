using System.Text.Json.Serialization;

namespace EnvimixWebAPI.Models;

public sealed class TitleStats
{
    [JsonPropertyName(nameof(EnvimixCompletionPercentage))] public required float EnvimixCompletionPercentage { get; set; }
    [JsonPropertyName(nameof(DefaultCarCompletionPercentage))] public required float DefaultCarCompletionPercentage { get; set; }
    [JsonPropertyName(nameof(GlobalCompletionPercentage))] public required float GlobalCompletionPercentage { get; set; }
    [JsonPropertyName(nameof(Players))] public required Dictionary<string, TitleUserInfo> Players { get; set; }
    [JsonPropertyName(nameof(Combinations))] public required Dictionary<string, Dictionary<string, CombinationStat>> Combinations { get; set; }
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
    [JsonPropertyName(nameof(CombinationRecordCount))] public required Dictionary<string, CombinationRecordCount> CombinationRecordCount { get; set; }
    [JsonPropertyName(nameof(EnvimixCombinationMostSkillpoints))] public required Dictionary<string, List<PlayerScore>> EnvimixCombinationMostSkillpoints { get; set; }
    [JsonPropertyName(nameof(EnvimixCombinationMostActivityPoints))] public required Dictionary<string, List<PlayerScore>> EnvimixCombinationMostActivityPoints { get; set; }
    [JsonPropertyName(nameof(EnvimixCombinationCompletion))] public required Dictionary<string, List<PlayerCompletion>> EnvimixCombinationCompletion { get; set; }
    [JsonPropertyName(nameof(DefaultCarCombinationMostSkillpoints))] public required Dictionary<string, List<PlayerScore>> DefaultCarCombinationMostSkillpoints { get; set; }
    [JsonPropertyName(nameof(DefaultCarCombinationMostActivityPoints))] public required Dictionary<string, List<PlayerScore>> DefaultCarCombinationMostActivityPoints { get; set; }
    [JsonPropertyName(nameof(DefaultCarCombinationCompletion))] public required Dictionary<string, List<PlayerCompletion>> DefaultCarCombinationCompletion { get; set; }
    [JsonPropertyName(nameof(GlobalCombinationMostSkillpoints))] public required Dictionary<string, List<PlayerScore>> GlobalCombinationMostSkillpoints { get; set; }
    [JsonPropertyName(nameof(GlobalCombinationMostActivityPoints))] public required Dictionary<string, List<PlayerScore>> GlobalCombinationMostActivityPoints { get; set; }
    [JsonPropertyName(nameof(GlobalCombinationCompletion))] public required Dictionary<string, List<PlayerCompletion>> GlobalCombinationCompletion { get; set; }
}
