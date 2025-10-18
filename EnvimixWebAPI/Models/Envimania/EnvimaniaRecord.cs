namespace EnvimixWebAPI.Models.Envimania;

public sealed class EnvimaniaRecord : EnvimaniaResult
{
    public EnvimaniaCheckpoint[] Checkpoints { get; set; } = [];
}