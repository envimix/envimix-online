using EnvimixWebAPI.Models.Envimania;
using System.Text.Json.Serialization;

namespace EnvimixWebAPI.Models;

public sealed class MapInfoResponse
{
    [JsonPropertyName(nameof(Name))] public required string Name { get; set; }
    [JsonPropertyName(nameof(Uid))] public required string Uid { get; set; }
    [JsonPropertyName(nameof(Collection))] public required string Collection { get; set; }
    [JsonPropertyName(nameof(AuthorLogin))] public required string? AuthorLogin { get; set; }
    [JsonPropertyName(nameof(AuthorNickname))] public required string? AuthorNickname { get; set; }
    [JsonPropertyName(nameof(Laps))] public required int Laps { get; set; }
    [JsonPropertyName(nameof(AuthorTime))] public required int AuthorTime { get; set; }
    [JsonPropertyName(nameof(GoldTime))] public required int GoldTime { get; set; }
    [JsonPropertyName(nameof(SilverTime))] public required int SilverTime { get; set; }
    [JsonPropertyName(nameof(BronzeTime))] public required int BronzeTime { get; set; }
    [JsonPropertyName(nameof(DuckTime))] public required int? DuckTime { get; set; }
    [JsonPropertyName(nameof(STMTime))] public required int? STMTime { get; set; }
    [JsonPropertyName(nameof(TitlePack))] public required TitleInfo? TitlePack { get; set; }
    [JsonPropertyName(nameof(Campaign))] public required Campaign? Campaign { get; set; }
    [JsonPropertyName(nameof(Ratings))] public required List<FilteredRating> Ratings { get; init; }
    [JsonPropertyName(nameof(UserRatings))] public required List<FilteredRating> UserRatings { get; init; }
    [JsonPropertyName(nameof(Validations))] public required Dictionary<string, EnvimaniaRecordInfo> Validations { get; init; }
    [JsonPropertyName(nameof(Stars))] public required Dictionary<string, Star> Stars { get; init; }
    [JsonPropertyName(nameof(Skillpoints))] public required Dictionary<string, int[]> Skillpoints { get; init; }
}
