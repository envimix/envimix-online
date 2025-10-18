using System.ComponentModel.DataAnnotations;

namespace EnvimixWebAPI.Entities;

public sealed class UserHistoryEntity
{
    [StringLength(64)]
    public required string Id { get; set; }

    public DateTimeOffset? LastSeenOn { get; set; }

    [StringLength(255)]
    public string? PrevNickname { get; set; }

    public ZoneEntity? PrevZone { get; set; }

    [StringLength(255)]
    public string? PrevAvatarUrl { get; set; }

    [StringLength(64)]
    public string? PrevLanguage { get; set; }

    [StringLength(255)]
    public string? PrevDescription { get; set; }

    public float[]? PrevColor { get; set; }

    [StringLength(255)]
    public string? PrevSteamUserId { get; set; }

    public int? PrevFameStars { get; set; }
    public float? PrevLadderPoints { get; set; }
}
