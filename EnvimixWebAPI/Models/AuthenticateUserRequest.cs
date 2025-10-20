namespace EnvimixWebAPI.Models;

public sealed class AuthenticateUserRequest
{
    public required string Token { get; set; }
    public required UserInfo User { get; set; }
}
