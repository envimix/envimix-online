using EnvimixWebsite.Services;
using Microsoft.AspNetCore.Mvc;

namespace EnvimixWebsite.Endpoints;

internal static class UserEndpoints
{
    public static void Map(WebApplication app)
    {
        app.MapPost("/users/unreleased-access", SetUnreleasedAccess)
            .RequireAuthorization(Policies.AdminPolicy);
    }

    private static async Task<IResult> SetUnreleasedAccess(
        [FromForm] string login,
        [FromForm] bool enabled,
        IEnvimixService envimixService,
        CancellationToken cancellationToken)
    {
        await envimixService.SetUnreleasedAccessAsync(login, enabled, cancellationToken);
        return TypedResults.LocalRedirect($"/users/{Uri.EscapeDataString(login)}");
    }
}
