namespace EnvimixWebAPI.Models;

public sealed class UpdateDiscordUserRequest
{
    public required string Username { get; set; }
    public required string Nickname { get; set; }
    public required string? AvatarHash { get; set; }
    public required string? ManiaPlanetLogin { get; set; }
}
