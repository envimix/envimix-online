using System.Text.Json.Serialization;

namespace EnvimixWebAPI.Models;

public sealed class SkillpointStats
{
    [JsonPropertyName(nameof(EnvimixMostSkillpoints))] public required List<PlayerScore> EnvimixMostSkillpoints { get; set; }
    [JsonPropertyName(nameof(DefaultCarMostSkillpoints))] public required List<PlayerScore> DefaultCarMostSkillpoints { get; set; }
    [JsonPropertyName(nameof(GlobalMostSkillpoints))] public required List<PlayerScore> GlobalMostSkillpoints { get; set; }
    [JsonPropertyName(nameof(EnvimixCombinationMostSkillpoints))] public required Dictionary<string, List<PlayerScore>> EnvimixCombinationMostSkillpoints { get; set; }
    [JsonPropertyName(nameof(DefaultCarCombinationMostSkillpoints))] public required Dictionary<string, List<PlayerScore>> DefaultCarCombinationMostSkillpoints { get; set; }
    [JsonPropertyName(nameof(GlobalCombinationMostSkillpoints))] public required Dictionary<string, List<PlayerScore>> GlobalCombinationMostSkillpoints { get; set; }
}
