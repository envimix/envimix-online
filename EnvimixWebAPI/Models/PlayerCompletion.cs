using System.Text.Json.Serialization;

namespace EnvimixWebAPI.Models;

public sealed class PlayerCompletion
{
    [JsonPropertyName("L")] public required string Login { get; set; }
    [JsonPropertyName("S")] public required float Score { get; set; }
}