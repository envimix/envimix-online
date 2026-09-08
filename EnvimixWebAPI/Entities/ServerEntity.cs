using System.ComponentModel.DataAnnotations;

namespace EnvimixWebAPI.Entities;

public sealed class ServerEntity
{
    [StringLength(64)]
    public required string Id { get; set; }

    [StringLength(255)]
    public string? Name { get; set; }

    [StringLength(255)]
    public string? BanReason { get; set; }

    public DateTimeOffset RegisteredAt { get; set; }

    [StringLength(64)]
    public string? RegisteredById { get; set; }
    public UserEntity? RegisteredBy { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public ICollection<EnvimaniaSessionEntity> EnvimaniaSessions { get; } = [];
}
