using System.Text.Json.Serialization;

namespace EnvimixWebAPI.Models;

public sealed class FilteredRating
{
    [JsonPropertyName(nameof(Filter))] public required RatingFilter Filter { get; set; }
    [JsonPropertyName(nameof(Rating))] public required Rating Rating { get; set; }
}
