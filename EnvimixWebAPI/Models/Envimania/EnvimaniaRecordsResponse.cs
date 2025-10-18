using System.Text.Json.Serialization;

namespace EnvimixWebAPI.Models.Envimania;

public sealed class EnvimaniaRecordsResponse
{
    [JsonPropertyName(nameof(Filter))] public required EnvimaniaRecordFilter Filter { get; set; }
    [JsonPropertyName(nameof(Zone))] public required string Zone { get; set; }
    [JsonPropertyName(nameof(Records))] public required IEnumerable<EnvimaniaRecordInfo> Records { get; set; }
}
