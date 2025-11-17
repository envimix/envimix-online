using System.Text.Json.Serialization;

namespace EnvimixWebAPI.Models.Envimania;

public sealed class EnvimaniaRecordsResponse
{
    [JsonPropertyName(nameof(Filter))] public required EnvimaniaRecordFilter Filter { get; set; }
    [JsonPropertyName(nameof(Zone))] public required string Zone { get; set; }
    [JsonPropertyName(nameof(Records))] public required IEnumerable<EnvimaniaRecordInfo> Records { get; set; }
    [JsonPropertyName(nameof(Validation))] public required EnvimaniaRecordInfo[] Validation { get; set; }
    [JsonPropertyName(nameof(Skillpoints))] public required int[] Skillpoints { get; set; }
    [JsonPropertyName(nameof(TitlePackReleaseTimestamp))] public required string TitlePackReleaseTimestamp { get; set; }
}
