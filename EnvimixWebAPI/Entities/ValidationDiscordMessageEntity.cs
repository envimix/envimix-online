namespace EnvimixWebAPI.Entities;

public sealed class ValidationDiscordMessageEntity
{
    public ulong Id { get; set; }
    public RecordEntity? Record { get; set; }
}
