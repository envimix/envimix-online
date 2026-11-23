using EnvimixWebAPI.Models;
using EnvimixWebAPI.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EnvimixWebAPI.Endpoints;

public static class PlayerEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        group.WithTags("Player");
        group.MapGet("{login}", GetPlayer);
        group.MapPost("{login}/unreleased-access", SetUnreleasedAccess);
    }

    private static async Task<Results<Ok<PlayerInfo>, NotFound>> GetPlayer(
        string login,
        AppDbContext db,
        IEnvimaniaService envimaniaService,
        ClaimsPrincipal principal,
        HttpRequest request,
        CancellationToken cancellationToken)
    {
        var player = await db.Users
            .Where(x => x.Id == login)
            .Select(x => new
            {
                Login = x.Id,
                x.Nickname,
                Zone = x.Zone == null ? null : x.Zone.Name,
                RecordCount = x.Records.Count,
                x.CanAccessUnreleased
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (player is null)
        {
            return TypedResults.NotFound();
        }

        // only admins are told who has access to unreleased campaigns
        var isAdmin = await envimaniaService.HasAdminAccessAsync(
            principal, EnvimaniaEndpoints.GetBearerToken(request), cancellationToken);

        var records = await RecordEndpoints.GetRecent(
            db.Records.Where(x => x.UserId == login), cancellationToken);
        return TypedResults.Ok(new PlayerInfo(
            player.Login,
            player.Nickname,
            player.Zone,
            player.RecordCount,
            records,
            isAdmin ? player.CanAccessUnreleased : null));
    }

    private static async Task<Results<Ok, NotFound, ForbidHttpResult>> SetUnreleasedAccess(
        string login,
        SetUnreleasedAccessRequest body,
        AppDbContext db,
        IEnvimaniaService envimaniaService,
        ClaimsPrincipal principal,
        HttpRequest request,
        CancellationToken cancellationToken)
    {
        if (!await envimaniaService.HasAdminAccessAsync(
            principal, EnvimaniaEndpoints.GetBearerToken(request), cancellationToken))
        {
            return TypedResults.Forbid();
        }

        var updated = await db.Users
            .Where(x => x.Id == login)
            .ExecuteUpdateAsync(x => x.SetProperty(user => user.CanAccessUnreleased, body.Enabled), cancellationToken);

        return updated == 0
            ? TypedResults.NotFound()
            : TypedResults.Ok();
    }
}

public sealed record SetUnreleasedAccessRequest(bool Enabled);
