using System.Text.Json.Serialization;

namespace EnvimixWebAPI.Models.Envimania;

public sealed class EnvimaniaMapInfo
{
    [JsonPropertyName(nameof(Filter))] public required EnvimaniaRecordFilter Filter { get; set; }
    [JsonPropertyName(nameof(Validation))] public required EnvimaniaRecordInfo Validation { get; set; }
    [JsonPropertyName(nameof(PersonalBest))] public required EnvimaniaRecordInfo PersonalBest { get; set; }
}
