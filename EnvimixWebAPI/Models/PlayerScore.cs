using System.Text.Json.Serialization;

namespace EnvimixWebAPI.Models;

public sealed class PlayerScore
{
    [JsonPropertyName("L")] public required string Login { get; set; }
    [JsonPropertyName("S")] public required int Score { get; set; }
}