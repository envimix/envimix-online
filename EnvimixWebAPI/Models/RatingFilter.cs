using EnvimixWebAPI.Models.Envimania;
using System.Text.Json.Serialization;

namespace EnvimixWebAPI.Models;

public sealed class RatingFilter
{
    [JsonPropertyName(nameof(Car))] public required string Car { get; set; }
    [JsonPropertyName(nameof(Gravity))] public int Gravity { get; set; }
    [JsonPropertyName(nameof(Type))] public EnvimaniaLeaderboardType Type { get; set; } = EnvimaniaLeaderboardType.Time; // why is it still required in endpoints man
}
