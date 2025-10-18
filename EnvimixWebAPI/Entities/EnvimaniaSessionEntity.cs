namespace EnvimixWebAPI.Entities;

public sealed class EnvimaniaSessionEntity
{
    public int Id { get; private set; }
    public required Guid Guid { get; set; }
    public required MapEntity Map { get; set; }
    public required ServerEntity Server { get; set; }
    public EnvimaniaSessionTokenEntity? EnvimaniaSessionToken { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset EndedAt { get; set; }
    public bool FinishedGracefully { get; set; }
}
