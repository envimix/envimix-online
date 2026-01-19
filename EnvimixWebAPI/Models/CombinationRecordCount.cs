using System.Text.Json.Serialization;

namespace EnvimixWebAPI.Models;

public sealed class CombinationRecordCount
{
    [JsonPropertyName("E")] public int Envimix { get; set; }
    [JsonPropertyName("D")] public int DefaultCar { get; set; }
    [JsonPropertyName("G")] public int Global { get; set; }
}
