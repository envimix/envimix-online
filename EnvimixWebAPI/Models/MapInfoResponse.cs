using EnvimixWebAPI.Dtos;
using EnvimixWebAPI.Models.Envimania;
using System.Text.Json.Serialization;

namespace EnvimixWebAPI.Models;

public sealed class MapInfoResponse
{
    [JsonPropertyName(nameof(Name))] public required string Name { get; set; }
    [JsonPropertyName(nameof(Uid))] public required string Uid { get; set; }
    [JsonPropertyName(nameof(TitlePack))] public required TitleDto? TitlePack { get; set; }
    [JsonPropertyName(nameof(Envimania))] public required EnvimaniaMapInfo[] Envimania { get; set; }
}
