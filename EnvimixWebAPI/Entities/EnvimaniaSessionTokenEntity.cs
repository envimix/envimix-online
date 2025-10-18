using System.ComponentModel.DataAnnotations;

namespace EnvimixWebAPI.Entities;

public sealed class EnvimaniaSessionTokenEntity
{
    [StringLength(64)]
    public required string Id { get; set; }

    public required EnvimaniaSessionEntity Session { get; set; }
    public required DateTimeOffset ExpiresAt { get; set; }
}