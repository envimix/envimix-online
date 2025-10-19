namespace EnvimixWebAPI.Models.Envimania;

public sealed class EnvimaniaRegistrationRequest
{
    public required string ServerLogin { get; set; }
    public string? ServerToken { get; set; }
}
