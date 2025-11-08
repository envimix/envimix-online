using System.Text.Json.Serialization;

namespace EnvimixWebAPI.Models;

public sealed class AuthenticateUserResponse
{
    [JsonPropertyName(nameof(Login))] public required string Login { get; set; }
    [JsonPropertyName(nameof(Token))] public required string Token { get; set; }
    [JsonPropertyName(nameof(IsAdmin))] public required bool IsAdmin { get; set; }
}
