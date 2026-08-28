using System.Text.Json.Serialization;

namespace EnvimixWebAPI.Models.Envimania;

public sealed class EnvimaniaSessionTokenResponse
{
    [JsonPropertyName(nameof(SessionToken))] public required string SessionToken { get; init; }
    [JsonPropertyName(nameof(ExpiresAt))] public required long ExpiresAt { get; init; }
}
