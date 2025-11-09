using System.Text.Json.Serialization;

namespace EnvimixWebAPI.Models;

public sealed class Star
{
    [JsonPropertyName(nameof(Login))] public required string Login { get; set; }
    [JsonPropertyName(nameof(Nickname))] public required string Nickname { get; set; }
}
