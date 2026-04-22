namespace EnvimixWebAPI.Models.Envimania;

public sealed class EnvimaniaRemoveRecordRequest
{
    public required string MapUid { get; set; }
    public required string Login { get; set; }
    public required string CarId { get; set; }
    public required int Gravity { get; set; }
    public required int Laps { get; set; }
    public required int Time { get; set; }
}
