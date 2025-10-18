using System.Text.Json.Serialization;

namespace EnvimixWebAPI.Models.Envimania;

public sealed class EnvimaniaRecordFilter
{
    [JsonPropertyName(nameof(Car))] public required string Car { get; set; }
    [JsonPropertyName(nameof(Gravity))] public int Gravity { get; set; }
    [JsonPropertyName(nameof(Laps))] public int Laps { get; set; } = 1;
    [JsonPropertyName(nameof(Type))] public EnvimaniaLeaderboardType Type { get; set; } = EnvimaniaLeaderboardType.Time; // why is it still required in endpoints man
}
