using System.Text.Json.Serialization;

namespace EnvimixWebAPI.Models.Envimania;

public sealed class EnvimaniaSessionUser
{
    [JsonPropertyName(nameof(Login))] public required string Login { get; init; }
    [JsonPropertyName(nameof(Ratings))] public required List<FilteredRating> Ratings { get; init; }
}
