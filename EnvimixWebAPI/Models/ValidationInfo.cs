using System.Text.Json.Serialization;

namespace EnvimixWebAPI.Models;

public sealed class ValidationInfo
{
    [JsonPropertyName("L")] public required string Login { get; set; }
    [JsonPropertyName("N")] public required string Nickname { get; set; }
    [JsonPropertyName("D")] public required string DrivenAt { get; set; }
}
