using System.Text.Json.Serialization;

namespace EnvimixWebAPI.Models;

public sealed class RatingClientResponse
{
    [JsonPropertyName(nameof(Rating))] public required FilteredRating Rating { get; init; }
}
