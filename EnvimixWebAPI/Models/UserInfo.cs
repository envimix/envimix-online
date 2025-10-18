using System.Text.Json.Serialization;

namespace EnvimixWebAPI.Models;

public sealed class UserInfo
{
    [JsonPropertyName(nameof(Login))] public required string Login { get; set; }
    [JsonPropertyName(nameof(Nickname))] public required string Nickname { get; set; }
    [JsonPropertyName(nameof(Zone))] public required string Zone { get; set; }
    [JsonPropertyName(nameof(AvatarUrl))] public required string AvatarUrl { get; set; }
    [JsonPropertyName(nameof(Language))] public required string Language { get; set; }
    [JsonPropertyName(nameof(Description))] public required string Description { get; set; }
    [JsonPropertyName(nameof(Color))] public required float[] Color { get; set; }
    [JsonPropertyName(nameof(SteamUserId))] public required string SteamUserId { get; set; }
    [JsonPropertyName(nameof(FameStars))] public required int FameStars { get; set; }
    [JsonPropertyName(nameof(LadderPoints))] public required float LadderPoints { get; set; }
}
