using System.Text.Json.Serialization;

namespace EnvimixWebAPI.Models;

public record Rating(
    [property: JsonPropertyName("Difficulty")] float? Difficulty,
    [property: JsonPropertyName("Quality")] float? Quality);
