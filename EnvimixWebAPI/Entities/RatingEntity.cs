namespace EnvimixWebAPI.Entities;

public sealed class RatingEntity
{
    public int Id { get; set; }
    public required UserEntity User { get; set; }
    public required MapEntity Map { get; set; }
    public required CarEntity Car { get; set; }
    public string CarId { get; set; } = null!;
    public required int Gravity { get; set; }
    public float? Difficulty { get; set; }
    public float? Quality { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    public ServerEntity? Server { get; set; }
    public string? ServerId { get; set; }
}
