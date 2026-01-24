namespace EnvimixWebAPI.Entities;

public sealed class MapDataEntity
{
    public int Id { get; set; }
    public DateTimeOffset LastModifiedAt { get; set; } = DateTimeOffset.UtcNow;
    public required byte[] Data { get; set; }
}
