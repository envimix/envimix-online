using System.Text.Json.Serialization;

namespace EnvimixWebAPI.Models;

public sealed class PlayerMedals
{
    [JsonPropertyName("L")] public required string Login { get; set; }
    [JsonPropertyName("D")] public required int Ducks { get; set; }
    [JsonPropertyName("ST")] public required int STMs { get; set; }
    [JsonPropertyName("SG")] public required int SuperGolds { get; set; }
    [JsonPropertyName("SS")] public required int SuperSilvers { get; set; }
    [JsonPropertyName("SB")] public required int SuperBronzes { get; set; }
    [JsonPropertyName("A")] public required int AuthorMedals { get; set; }
    [JsonPropertyName("G")] public required int GoldMedals { get; set; }
    [JsonPropertyName("S")] public required int SilverMedals { get; set; }
    [JsonPropertyName("B")] public required int BronzeMedals { get; set; }
}