using System.Text.Json.Serialization;

namespace EnvimixWebAPI.Models;

public sealed class PlayerScore
{
    [JsonPropertyName(nameof(PlayerLogin))] public required string PlayerLogin { get; set; }
    [JsonPropertyName(nameof(PlayerNickname))] public required string PlayerNickname { get; set; }
    [JsonPropertyName(nameof(Score))] public required int Score { get; set; }
}