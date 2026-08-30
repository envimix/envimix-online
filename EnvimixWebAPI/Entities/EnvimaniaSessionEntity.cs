using System.ComponentModel.DataAnnotations;

namespace EnvimixWebAPI.Entities;

public sealed class EnvimaniaSessionEntity
{
    public required Guid Id { get; set; }
    public required MapEntity Map { get; set; }
    public required ServerEntity Server { get; set; }

    [StringLength(255)]
    public string? ServerModeName { get; set; }

    [StringLength(64)]
    public string? TitleId { get; set; }

    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? EndedAt { get; set; }
    public bool FinishedGracefully { get; set; }
}
