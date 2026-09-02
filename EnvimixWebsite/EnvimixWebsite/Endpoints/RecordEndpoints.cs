using EnvimixWebsite.Services;
using Microsoft.AspNetCore.Mvc;

namespace EnvimixWebsite.Endpoints;

internal static class RecordEndpoints
{
    public static void Map(WebApplication app)
    {
        app.MapPost("/records/remove", RemoveRecord)
            .RequireAuthorization(Policies.AdminPolicy);
        app.MapPost("/records/revert", RevertRecord)
            .RequireAuthorization(Policies.AdminPolicy);
    }

    private static async Task<IResult> RemoveRecord(
        [FromForm] Guid? sessionId,
        [FromForm] bool? redirectToUser,
        [FromForm] bool? redirectToMap,
        [FromForm] string mapUid,
        [FromForm] string login,
        [FromForm] string carId,
        [FromForm] int gravity,
        [FromForm] int laps,
        [FromForm] int time,
        IEnvimixService envimixService,
        CancellationToken cancellationToken)
    {
        await envimixService.RemoveRecordAsync(mapUid, login, carId, gravity, laps, time, cancellationToken);
        return TypedResults.LocalRedirect(sessionId.HasValue
            ? $"/envimania/sessions/{sessionId}"
            : redirectToUser == true
                ? $"/users/{Uri.EscapeDataString(login)}"
            : redirectToMap == true
                ? $"/maps/{Uri.EscapeDataString(mapUid)}"
            : $"/records/{Uri.EscapeDataString(mapUid)}/{Uri.EscapeDataString(carId)}/{Uri.EscapeDataString(login)}/{time}");
    }

    private static async Task<IResult> RevertRecord(
        [FromForm] Guid? sessionId,
        [FromForm] bool? redirectToUser,
        [FromForm] bool? redirectToMap,
        [FromForm] string mapUid,
        [FromForm] string login,
        [FromForm] string carId,
        [FromForm] int gravity,
        [FromForm] int laps,
        [FromForm] int time,
        IEnvimixService envimixService,
        CancellationToken cancellationToken)
    {
        await envimixService.RevertRecordAsync(mapUid, login, carId, gravity, laps, time, cancellationToken);
        return TypedResults.LocalRedirect(sessionId.HasValue
            ? $"/envimania/sessions/{sessionId}"
            : redirectToUser == true
                ? $"/users/{Uri.EscapeDataString(login)}"
            : redirectToMap == true
                ? $"/maps/{Uri.EscapeDataString(mapUid)}"
            : $"/records/{Uri.EscapeDataString(mapUid)}/{Uri.EscapeDataString(carId)}/{Uri.EscapeDataString(login)}/{time}");
    }
}
