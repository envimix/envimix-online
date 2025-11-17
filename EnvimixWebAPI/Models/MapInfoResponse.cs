using EnvimixWebAPI.Dtos;
using EnvimixWebAPI.Models.Envimania;
using System.Text.Json.Serialization;

namespace EnvimixWebAPI.Models;

public sealed class MapInfoResponse
{
    [JsonPropertyName(nameof(Name))] public required string Name { get; set; }
    [JsonPropertyName(nameof(Uid))] public required string Uid { get; set; }
    [JsonPropertyName(nameof(TitlePack))] public required TitleDto? TitlePack { get; set; }
    [JsonPropertyName(nameof(Ratings))] public required List<FilteredRating> Ratings { get; init; }
    [JsonPropertyName(nameof(UserRatings))] public required List<FilteredRating> UserRatings { get; init; }
    [JsonPropertyName(nameof(Validations))] public required Dictionary<string, EnvimaniaRecordInfo> Validations { get; init; }
    [JsonPropertyName(nameof(Stars))] public required Dictionary<string, Star> Stars { get; init; }
    [JsonPropertyName(nameof(Skillpoints))] public required Dictionary<string, int[]> Skillpoints { get; init; }
}
