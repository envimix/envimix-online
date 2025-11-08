namespace EnvimixWebAPI.Entities;

public sealed class GhostEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTimeOffset LastModifiedAt { get; set; } = DateTimeOffset.UtcNow;
    public required byte[] Data { get; set; }
}
