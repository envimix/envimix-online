using System.Text.Json.Serialization;

namespace EnvimixWebAPI.Models;

public sealed class MapInfo
{
    [JsonPropertyName(nameof(Name))] public required string Name { get; set; }
    [JsonPropertyName(nameof(Uid))] public required string Uid { get; set; }
    [JsonPropertyName(nameof(Collection))] public string? Collection { get; set; }
    [JsonPropertyName(nameof(AuthorLogin))] public string? AuthorLogin { get; set; }
    [JsonPropertyName(nameof(AuthorNickname))] public string? AuthorNickname { get; set; }
    [JsonPropertyName(nameof(Order))] public int? Order { get; set; }
    [JsonPropertyName(nameof(Campaign))] public string? Campaign { get; set; }
    [JsonPropertyName(nameof(Laps))] public int Laps { get; set; }
    [JsonPropertyName(nameof(AuthorTime))] public int AuthorTime { get; set; }
    [JsonPropertyName(nameof(GoldTime))] public int GoldTime { get; set; }
    [JsonPropertyName(nameof(SilverTime))] public int SilverTime { get; set; }
    [JsonPropertyName(nameof(BronzeTime))] public int BronzeTime { get; set; }
}
