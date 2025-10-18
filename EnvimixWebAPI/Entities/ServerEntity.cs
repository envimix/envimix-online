using System.ComponentModel.DataAnnotations;

namespace EnvimixWebAPI.Entities;

public sealed class ServerEntity
{
    [StringLength(64)]
    public required string Id { get; set; }

    [StringLength(255)]
    public string? BanReason { get; set; }

    public ICollection<EnvimaniaSessionEntity> EnvimaniaSessions { get; } = [];
}