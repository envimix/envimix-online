using System.Security.Claims;

namespace EnvimixWebAPI.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static string GetName(this ClaimsPrincipal principal)
    {
        return principal.Identity?.Name ?? "[unknown]";
    }
}
