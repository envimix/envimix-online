using System.ComponentModel.DataAnnotations;

namespace EnvimixWebAPI.Entities;

public sealed class DiscordUserEntity
{
    [StringLength(16)]
    public required string Id { get; set; }

    [StringLength(32, MinimumLength = 2)]
    public string? Username { get; set; }

    [StringLength(32)]
    public string? Nickname { get; set; }

    [StringLength(255)]
    public string? AvatarHash { get; set; }

    public ICollection<UserEntity> Users { get; } = [];
}
