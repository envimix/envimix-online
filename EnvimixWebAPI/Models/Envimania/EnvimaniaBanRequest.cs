namespace EnvimixWebAPI.Models.Envimania;

public sealed class EnvimaniaBanRequest
{
    public required string ServerLogin { get; set; }
    public string? Reason { get; set; }
}
