namespace EnvimixWebAPI.Models;

public sealed class UpdateUserRequest
{
    public required string Nickname { get; set; }
    public required string Zone { get; set; }
    public required string? DiscordSnowflake { get; set; }
}
