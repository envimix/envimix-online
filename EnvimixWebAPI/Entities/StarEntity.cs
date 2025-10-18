namespace EnvimixWebAPI.Entities;

public sealed class StarEntity
{
    public int Id { get; set; }
    public required UserEntity User { get; set; }
    public required MapEntity Map { get; set; }
    public required CarEntity Car { get; set; }
    public required int Gravity { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }
}
