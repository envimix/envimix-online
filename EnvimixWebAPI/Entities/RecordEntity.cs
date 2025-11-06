using Microsoft.EntityFrameworkCore;

namespace EnvimixWebAPI.Entities;

[Index(nameof(Gravity))]
[Index(nameof(Laps))]
public sealed class RecordEntity
{
    public int Id { get; set; }
    public required UserEntity User { get; set; }
    public required MapEntity Map { get; set; }
    public required CarEntity Car { get; set; }
    public required int Gravity { get; set; }
    public required DateTimeOffset DrivenAt { get; set; }

    public EnvimaniaSessionEntity? Session { get; set; } = null!;
    public Guid? SessionId { get; set; }

    public required int Laps { get; set; }

    public byte[]? GhostData { get; set; }
    public DateTimeOffset? ServersideDrivenAt { get; set; }

    public ICollection<CheckpointEntity> Checkpoints { get; } = [];
}
