namespace EnvimixWebAPI.Models.Envimania;

public sealed class EnvimaniaBanResponse
{
    public required string ServerLogin { get; set; }
    public required bool Banned { get; set; }
    public required string Reason { get; set; }
}
