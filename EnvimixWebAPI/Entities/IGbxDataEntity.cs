namespace EnvimixWebAPI.Entities;

public interface IGbxDataEntity
{
    Guid Id { get; }
    DateTimeOffset LastModifiedAt { get; set; }
    byte[] Data { get; set; }
    bool IpsRemoved { get; set; }
    byte[]? DataBackup { get; set; }
}
