namespace EnvimixWebAPI.Models;

public sealed record ActionForbiddenResponse(string Message)
{
    public static ActionForbiddenResponse ServerLoginBanned { get; } = new("Server login is banned.");
    public static ActionForbiddenResponse UserLoginBanned { get; } = new("User login is banned.");
}
