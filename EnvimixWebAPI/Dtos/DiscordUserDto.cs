namespace EnvimixWebAPI.Dtos;

public sealed class DiscordUserDto
{
    public required string Snowflake { get; set; }
    public required string? Username { get; set; }
    public required string? Nickname { get; set; }
    public required string? AvatarHash { get; set; }
}
