namespace EnvimixWebAPI.Models.Envimania;

public sealed class EnvimaniaSessionRecordBulkRequest
{
    public required EnvimaniaSessionRecordRequest[] Requests { get; set; }
}
