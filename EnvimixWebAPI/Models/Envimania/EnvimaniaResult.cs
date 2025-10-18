namespace EnvimixWebAPI.Models.Envimania;

public abstract class EnvimaniaResult
{
    public required int Time { get; set; }
    public required int Score { get; set; }
    public required int NbRespawns { get; set; }
    public required float Distance { get; set; }
    public required float Speed { get; set; }
}