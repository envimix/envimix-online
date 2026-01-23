using System.Text.Json.Serialization;

namespace EnvimixWebAPI.Models;

public sealed class TitleReleaseInfo
{
    [JsonPropertyName(nameof(ReleasedAt))] public required string ReleasedAt { get; init; }
    [JsonPropertyName(nameof(Key))] public required string Key { get; init; }
    [JsonPropertyName(nameof(CampaignsReleasedAt))] public required Dictionary<string, string> CampaignsReleasedAt { get; init; }
}
