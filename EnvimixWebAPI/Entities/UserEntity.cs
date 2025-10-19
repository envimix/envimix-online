using System.ComponentModel.DataAnnotations;

namespace EnvimixWebAPI.Entities;

public sealed class UserEntity
{
    [StringLength(64)]
    public required string Id { get; set; }

    [StringLength(255)]
    public string? Nickname { get; set; }
    public ZoneEntity? Zone { get; set; }
    public int? ZoneId { get; set; }

    [StringLength(255)]
    public string? AvatarUrl { get; set; }

    [StringLength(64)]
    public string? Language { get; set; }

    [StringLength(255)]
    public string? Description { get; set; }

    public float[]? Color { get; set; }

    [StringLength(255)]
    public string? SteamUserId { get; set; }

    public int? FameStars { get; set; }
    public float? LadderPoints { get; set; }
    public bool IsInsider { get; set; }
    public bool IsAdmin { get; set; }
    public DiscordUserEntity? DiscordUser { get; set; }
    //public DateTime? LastSeenOn { get; set; }
    //public Server? LastSeenOnServer { get; set; }

    /// <summary>
    /// If the player has personally authorized with OAuth2 or by opening an Envimix title pack.
    /// </summary>
    public bool Interested { get; set; }

    public ICollection<RecordEntity> Records { get; } = [];
}
