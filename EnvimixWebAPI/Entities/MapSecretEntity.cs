using Microsoft.EntityFrameworkCore;

namespace EnvimixWebAPI.Entities;

[Index(nameof(MapId), nameof(UserId), IsUnique = true)]
public sealed class MapSecretEntity
{
    public int Id { get; set; }
    public required UserEntity User { get; set; }
    public string UserId { get; set; } = null!;
    public required MapEntity Map { get; set; }
    public string MapId { get; set; } = null!;
    public required DateTimeOffset FoundAt { get; set; }
}
