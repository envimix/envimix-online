using System.Text.Json.Serialization;

namespace EnvimixWebAPI.Models;

public sealed class TitleUserInfo
{
    [JsonPropertyName("N")] public required string Nickname { get; set; }
    [JsonPropertyName("Z")] public required string Zone { get; set; }
}
