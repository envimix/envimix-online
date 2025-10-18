namespace EnvimixWebAPI.Models.Envimania;

public sealed class EnvimaniaSessionRequest
{
    public required MapInfo Map { get; set; }
    public required UserInfo[] Players { get; set; }
    public required string[] Cars { get; set; }
}