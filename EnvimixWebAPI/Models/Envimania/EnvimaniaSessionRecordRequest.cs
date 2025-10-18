namespace EnvimixWebAPI.Models.Envimania;

public sealed class EnvimaniaSessionRecordRequest
{
    public required UserInfo User { get; set; }
    public required string Car { get; set; }
    public required int Gravity { get; set; }
    public required int Laps { get; set; }
    public required EnvimaniaRecord Record { get; set; }
    public required int PreferenceNumber { get; set; }
}
