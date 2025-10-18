using System.Text.Json.Serialization;

namespace EnvimixWebAPI.Models.Envimania;

public sealed class EnvimaniaSessionRecordResponse
{
    [JsonPropertyName(nameof(UpdatedRecords))] public required List<EnvimaniaRecordsResponse> UpdatedRecords { get; set; }
}
