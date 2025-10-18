namespace EnvimixWebAPI.Dtos;

public sealed class UserDto
{
    public required string Login { get; set; }
    public required string? Nickname { get; set; }
    public required string? Zone { get; set; }
    public required DiscordUserDto? Discord { get; set; }
}
