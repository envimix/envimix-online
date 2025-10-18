namespace EnvimixWebAPI.Entities;

public sealed class CheckpointEntity
{
    public int Id { get; set; }
    public required RecordEntity Record { get; set; }
    public required int Time { get; set; }
    public required int Score { get; set; }
    public required int NbRespawns { get; set; }
    public required float Distance { get; set; }
    public required float Speed { get; set; }
}
