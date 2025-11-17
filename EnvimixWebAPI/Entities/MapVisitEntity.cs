namespace EnvimixWebAPI.Entities;

public sealed class MapVisitEntity
{
    public int Id { get; set; }
    public required UserEntity User { get; set; }
    public required MapEntity Map { get; set; }
    public DateTimeOffset VisitedAt { get; set; }
}
