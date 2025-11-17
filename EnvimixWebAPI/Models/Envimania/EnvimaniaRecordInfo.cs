using System.Text.Json.Serialization;

namespace EnvimixWebAPI.Models.Envimania;

public sealed class EnvimaniaRecordInfo
{
    [JsonPropertyName(nameof(User))] public required UserInfo User { get; set; }
    [JsonPropertyName(nameof(Time))] public required int Time { get; set; }
    [JsonPropertyName(nameof(Score))] public required int Score { get; set; }
    [JsonPropertyName(nameof(NbRespawns))] public required int NbRespawns { get; set; }
    [JsonPropertyName(nameof(Distance))] public required float Distance { get; set; }
    [JsonPropertyName(nameof(Speed))] public required float Speed { get; set; }
    [JsonPropertyName(nameof(Verified))] public required bool Verified { get; set; }

    /// <summary>
    /// If the record is projected from different leaderboard.
    /// </summary>
    [JsonPropertyName(nameof(Projected))] public required bool Projected { get; set; }

    [JsonPropertyName(nameof(GhostUrl))] public required string GhostUrl { get; set; }

    [JsonPropertyName(nameof(DrivenAt))] public required string DrivenAt { get; set; }
}
