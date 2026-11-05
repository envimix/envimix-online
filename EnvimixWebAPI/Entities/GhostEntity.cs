namespace EnvimixWebAPI.Entities;

public sealed class GhostEntity : IGbxDataEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTimeOffset LastModifiedAt { get; set; } = DateTimeOffset.UtcNow;
    public required byte[] Data { get; set; }
    public bool IpsRemoved { get; set; }
    public byte[]? DataBackup { get; set; }
}
