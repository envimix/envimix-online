using System.Text.Json.Serialization;

namespace EnvimixWebAPI.Models;

public sealed class CompletionStats
{
    [JsonPropertyName(nameof(Players))] public required Dictionary<string, TitleUserInfo> Players { get; set; }
    [JsonPropertyName(nameof(EnvimixCompletionPercentage))] public required float EnvimixCompletionPercentage { get; set; }
    [JsonPropertyName(nameof(DefaultCarCompletionPercentage))] public required float DefaultCarCompletionPercentage { get; set; }
    [JsonPropertyName(nameof(GlobalCompletionPercentage))] public required float GlobalCompletionPercentage { get; set; }
    [JsonPropertyName(nameof(EnvimixCompletionPercentages))] public required Dictionary<string, float> EnvimixCompletionPercentages { get; set; }
    [JsonPropertyName(nameof(DefaultCarCompletionPercentages))] public required Dictionary<string, float> DefaultCarCompletionPercentages { get; set; }
    [JsonPropertyName(nameof(GlobalCompletionPercentages))] public required Dictionary<string, float> GlobalCompletionPercentages { get; set; }
    [JsonPropertyName(nameof(EnvimixCompletion))] public required List<PlayerCompletion> EnvimixCompletion { get; set; }
    [JsonPropertyName(nameof(DefaultCarCompletion))] public required List<PlayerMedals> DefaultCarCompletion { get; set; }
    [JsonPropertyName(nameof(GlobalCompletion))] public required List<PlayerCompletion> GlobalCompletion { get; set; }
    [JsonPropertyName(nameof(CombinationRecordCount))] public required Dictionary<string, CombinationRecordCount> CombinationRecordCount { get; set; }
    [JsonPropertyName(nameof(EnvimixCombinationCompletion))] public required Dictionary<string, List<PlayerCompletion>> EnvimixCombinationCompletion { get; set; }
    [JsonPropertyName(nameof(DefaultCarCombinationCompletion))] public required Dictionary<string, List<PlayerMedals>> DefaultCarCombinationCompletion { get; set; }
    [JsonPropertyName(nameof(GlobalCombinationCompletion))] public required Dictionary<string, List<PlayerCompletion>> GlobalCombinationCompletion { get; set; }
}
