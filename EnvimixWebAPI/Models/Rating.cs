using System.Text.Json.Serialization;

namespace EnvimixWebAPI.Models;

public readonly record struct Rating(
    [property: JsonPropertyName("Difficulty")] float? Difficulty,
    [property: JsonPropertyName("Quality")] float? Quality);
