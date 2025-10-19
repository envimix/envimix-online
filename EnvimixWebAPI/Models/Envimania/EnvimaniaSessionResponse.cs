using System.Text.Json.Serialization;

namespace EnvimixWebAPI.Models.Envimania;

public sealed class EnvimaniaSessionResponse
{
    [JsonPropertyName(nameof(ServerLogin))] public required string ServerLogin { get; set; }
    [JsonPropertyName(nameof(SessionToken))] public required string SessionToken { get; set; }
    [JsonPropertyName(nameof(Ratings))] public required List<FilteredRating> Ratings { get; init; }
    [JsonPropertyName(nameof(UserRatings))] public required Dictionary<string, List<FilteredRating>> UserRatings { get; init; }
    [JsonPropertyName(nameof(Validations))] public required Dictionary<string, EnvimaniaRecordInfo> Validations { get; init; }
}