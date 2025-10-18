using System.Text.Json.Serialization;

namespace EnvimixWebAPI.Models;

public sealed class RatingServerResponse
{
    [JsonPropertyName(nameof(Ratings))] public required List<FilteredRating> Ratings { get; init; }
}
