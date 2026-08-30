using EnvimixWebAPI.Models;
using EnvimixWebAPI.Models.Envimania;
using EnvimixWebAPI.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EnvimixWebAPI.Endpoints;

public static class EnvimaniaEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        group.WithTags("Envimania");

        group.MapPost("register", Register);
        group.MapDelete("servers/{serverLogin}", SoftDeleteServer);
        group.MapDelete("servers/{serverLogin}/wipe", WipeServer);
        group.MapDelete("servers/{serverLogin}/records", DeleteServerRecords);
        group.MapDelete("servers/{serverLogin}/ratings", DeleteServerRatings);
        group.MapPost("servers/{serverLogin}/ban", BanServer);
        group.MapPost("servers/{serverLogin}/unban", UnbanServer);
        group.MapGet("registered", GetRegistered);
        group.MapGet("servers", GetServers);
        group.MapGet("servers/{serverLogin}", GetServer);
        group.MapGet("sessions/{sessionId:guid}", GetSession);

        group.MapPost("ban", Ban).RequireAuthorization(Policies.SuperAdminPolicy);
        group.MapPost("unban", Unban).RequireAuthorization(Policies.SuperAdminPolicy);

        group.MapGet("records/{mapUid}/{car}", Records);

        group.MapPost("record", Record).RequireAuthorization(Policies.ManiaPlanetUserPolicy);

        MapSession(group.MapGroup("session"));

        group.MapPost("restore-validations", RestoreValidations).RequireAuthorization(Policies.SuperAdminPolicy);
        group.MapPost("restore-records", RestoreRecords).RequireAuthorization(Policies.SuperAdminPolicy);

        group.MapPost("record/remove", RemoveRecord).RequireAuthorization(Policies.AdminPolicy);
        group.MapPost("record/revert", RevertRecord).RequireAuthorization(Policies.AdminPolicy);
    }

    private static void MapSession(RouteGroupBuilder group)
    {
        group.MapPost("", Session).RequireRateLimiting("20Per10Minutes");
        group.MapGet("status", SessionStatus).RequireAuthorization(Policies.EnvimaniaSessionPolicy);
        group.MapPost("extend", SessionExtend).RequireAuthorization(Policies.EnvimaniaSessionPolicy);
        group.MapPost("record", SessionRecord).RequireAuthorization(Policies.EnvimaniaSessionPolicy);
        group.MapPost("records", SessionRecordsPost).RequireAuthorization(Policies.EnvimaniaSessionPolicy);
        group.MapGet("records/{car}", SessionRecordsGet).RequireAuthorization(Policies.EnvimaniaSessionPolicy);
        group.MapPost("rate", SessionRate).RequireAuthorization(Policies.EnvimaniaSessionPolicy);
        group.MapPost("user", SessionUser).RequireAuthorization(Policies.EnvimaniaSessionPolicy);
        group.MapPost("users", SessionUsers).RequireAuthorization(Policies.EnvimaniaSessionPolicy);
        group.MapPost("close", SessionClose).RequireAuthorization(Policies.EnvimaniaSessionPolicy);
    }

    private static async Task<Results<Ok<EnvimaniaServer>, BadRequest<ValidationFailureResponse>, UnprocessableEntity<ActionUnprocessableResponse>, ForbidHttpResult>> Register(
        [FromBody] EnvimaniaRegistrationRequest registerRequest,
        HttpRequest request,
        IEnvimaniaService envimaniaService,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var authorization = request.Headers.Authorization.ToString();
        var identityAccessToken = authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? authorization["Bearer ".Length..].Trim()
            : null;

        var result = await envimaniaService.RegisterAsync(registerRequest, principal, identityAccessToken, cancellationToken);

        return result.Match<Results<Ok<EnvimaniaServer>, BadRequest<ValidationFailureResponse>, UnprocessableEntity<ActionUnprocessableResponse>, ForbidHttpResult>>(
            validResponse => TypedResults.Ok(validResponse), // TODO: use Created here instead
            validationFailure => TypedResults.BadRequest(validationFailure),
            actionUnprocessable => TypedResults.UnprocessableEntity(actionUnprocessable),
            actionForbidden => TypedResults.Forbid()
        );
    }

    private static async Task<Ok<string[]>> GetRegistered(
        [FromQuery] string[] serverLogin,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var registered = await db.Servers
            .Where(x => x.DeletedAt == null && serverLogin.Contains(x.Id))
            .Select(x => x.Id)
            .ToArrayAsync(cancellationToken);

        return TypedResults.Ok(registered);
    }

    private static async Task<Results<NoContent, NotFound, ForbidHttpResult>> SoftDeleteServer(
        string serverLogin,
        HttpRequest request,
        IEnvimaniaService envimaniaService,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var result = await envimaniaService.SoftDeleteServerAsync(
            serverLogin,
            principal,
            GetBearerToken(request),
            cancellationToken);

        return result.Match<Results<NoContent, NotFound, ForbidHttpResult>>(
            deleted => deleted ? TypedResults.NoContent() : TypedResults.NotFound(),
            _ => TypedResults.Forbid());
    }

    private static async Task<Results<Ok<EnvimaniaServerOperationResponse>, ForbidHttpResult>> WipeServer(
        string serverLogin,
        HttpRequest request,
        IEnvimaniaService envimaniaService,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var result = await envimaniaService.WipeServerAsync(
            serverLogin, principal, GetBearerToken(request), cancellationToken);

        return result.Match<Results<Ok<EnvimaniaServerOperationResponse>, ForbidHttpResult>>(
            response => TypedResults.Ok(response),
            _ => TypedResults.Forbid());
    }

    private static async Task<Results<Ok<EnvimaniaServerOperationResponse>, ForbidHttpResult>> DeleteServerRecords(
        string serverLogin,
        HttpRequest request,
        IEnvimaniaService envimaniaService,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var result = await envimaniaService.DeleteServerRecordsAsync(
            serverLogin, principal, GetBearerToken(request), cancellationToken);

        return result.Match<Results<Ok<EnvimaniaServerOperationResponse>, ForbidHttpResult>>(
            response => TypedResults.Ok(response),
            _ => TypedResults.Forbid());
    }

    private static async Task<Results<Ok<EnvimaniaServerOperationResponse>, ForbidHttpResult>> DeleteServerRatings(
        string serverLogin,
        HttpRequest request,
        IEnvimaniaService envimaniaService,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var result = await envimaniaService.DeleteServerRatingsAsync(
            serverLogin, principal, GetBearerToken(request), cancellationToken);

        return result.Match<Results<Ok<EnvimaniaServerOperationResponse>, ForbidHttpResult>>(
            response => TypedResults.Ok(response),
            _ => TypedResults.Forbid());
    }

    private static async Task<Results<NoContent, NotFound, ForbidHttpResult, BadRequest<string>>> BanServer(
        string serverLogin,
        [FromBody] EnvimaniaServerBanRequest banRequest,
        HttpRequest request,
        IEnvimaniaService envimaniaService,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var reason = banRequest.Reason.Trim();
        if (reason.Length is 0 or > 255)
        {
            return TypedResults.BadRequest("Ban reason must be between 1 and 255 characters.");
        }

        var result = await envimaniaService.BanServerAsync(
            serverLogin, reason, principal, GetBearerToken(request), cancellationToken);

        return result.Match<Results<NoContent, NotFound, ForbidHttpResult, BadRequest<string>>>(
            banned => banned ? TypedResults.NoContent() : TypedResults.NotFound(),
            _ => TypedResults.Forbid());
    }

    private static async Task<Results<NoContent, NotFound, ForbidHttpResult>> UnbanServer(
        string serverLogin,
        HttpRequest request,
        IEnvimaniaService envimaniaService,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var result = await envimaniaService.UnbanServerAsync(
            serverLogin, principal, GetBearerToken(request), cancellationToken);

        return result.Match<Results<NoContent, NotFound, ForbidHttpResult>>(
            unbanned => unbanned ? TypedResults.NoContent() : TypedResults.NotFound(),
            _ => TypedResults.Forbid());
    }

    private static async Task<Ok<EnvimaniaServerSummary[]>> GetServers(
        HttpRequest request,
        AppDbContext db,
        IEnvimaniaService envimaniaService,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var access = await envimaniaService.GetServerAccessAsync(
            "", principal, GetBearerToken(request), cancellationToken);

        var servers = await db.Servers
            .Where(x => access.CanAdminister || (x.BanReason == null && x.DeletedAt == null))
            .OrderBy(x => x.Id)
            .Select(x => new EnvimaniaServerSummary(
                x.Id,
                x.EnvimaniaSessions.Count,
                x.EnvimaniaSessions
                    .OrderByDescending(session => session.StartedAt)
                    .Select(session => (DateTimeOffset?)session.StartedAt)
                    .FirstOrDefault(),
                x.DeletedAt != null,
                x.BanReason != null))
            .ToArrayAsync(cancellationToken);

        return TypedResults.Ok(servers);
    }

    private static async Task<Results<Ok<EnvimaniaServerInfo>, NotFound>> GetServer(
        string serverLogin,
        int sessions,
        HttpRequest request,
        AppDbContext db,
        IEnvimaniaService envimaniaService,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var access = await envimaniaService.GetServerAccessAsync(
            serverLogin, principal, GetBearerToken(request), cancellationToken);

        var server = await db.Servers
            .Where(x => x.Id == serverLogin && (x.DeletedAt == null || access.CanAdminister))
            .Select(x => new
            {
                ServerLogin = x.Id,
                SessionCount = x.EnvimaniaSessions.Count,
                LastSeenAt = x.EnvimaniaSessions
                    .OrderByDescending(session => session.StartedAt)
                    .Select(session => (DateTimeOffset?)session.StartedAt)
                    .FirstOrDefault(),
                IsHidden = x.DeletedAt != null,
                IsBanned = x.BanReason != null
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (server is null)
        {
            return TypedResults.NotFound();
        }

        var recentSessions = await db.EnvimaniaSessions
            .Where(x => x.Server.Id == serverLogin)
            .OrderByDescending(x => x.StartedAt)
            .Take(Math.Clamp(sessions, 10, 500))
            .Select(x => new EnvimaniaServerSession(
                x.Id,
                x.Map.Id,
                x.Map.Name,
                x.StartedAt,
                x.EndedAt,
                x.FinishedGracefully))
            .ToArrayAsync(cancellationToken);

        return TypedResults.Ok(new EnvimaniaServerInfo(
            server.ServerLogin,
            server.SessionCount,
            server.LastSeenAt,
            recentSessions,
            server.IsHidden,
            server.IsBanned,
            access.CanDelete,
            access.CanAdminister));
    }

    private static async Task<Results<Ok<EnvimaniaSessionInfo>, NotFound>> GetSession(
        Guid sessionId,
        HttpRequest request,
        AppDbContext db,
        IEnvimaniaService envimaniaService,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var session = await db.EnvimaniaSessions
            .Where(x => x.Id == sessionId)
            .Select(x => new
            {
                x.Id,
                ServerLogin = x.Server.Id,
                ServerDeletedAt = x.Server.DeletedAt,
                MapUid = x.Map.Id,
                MapName = x.Map.Name,
                MapLaps = x.Map.Laps,
                x.StartedAt,
                x.EndedAt,
                x.FinishedGracefully
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (session is null)
        {
            return TypedResults.NotFound();
        }

        var access = await envimaniaService.GetServerAccessAsync(
            session.ServerLogin, principal, GetBearerToken(request), cancellationToken);
        if (session.ServerDeletedAt is not null && !access.CanAdminister)
        {
            return TypedResults.NotFound();
        }

        var records = await db.Records
            .Where(x => x.SessionId == sessionId)
            .OrderBy(x => x.Time)
            .ThenByDescending(x => x.Score)
            .Select(x => new EnvimaniaSessionRecord(
                x.UserId,
                x.User.Nickname,
                x.CarId,
                x.Gravity,
                x.Laps,
                x.Time,
                x.Score,
                x.NbRespawns,
                x.DrivenAt,
                x.Removed))
            .ToArrayAsync(cancellationToken);

        return TypedResults.Ok(new EnvimaniaSessionInfo(
            session.Id,
            session.ServerLogin,
            session.MapUid,
            session.MapName,
            session.MapLaps,
            session.StartedAt,
            session.EndedAt,
            session.FinishedGracefully,
            access.CanAdminister,
            records));
    }

    private static string? GetBearerToken(HttpRequest request)
    {
        var authorization = request.Headers.Authorization.ToString();
        return authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? authorization["Bearer ".Length..].Trim()
            : null;
    }

    private static async Task<Results<Ok<EnvimaniaBanResponse>, BadRequest<ValidationFailureResponse>, UnprocessableEntity<ActionUnprocessableResponse>>> Ban(
        [FromBody] EnvimaniaBanRequest banRequest,
        IEnvimaniaService envimaniaService,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var result = await envimaniaService.BanAsync(banRequest, principal, cancellationToken);

        return result.Match<Results<Ok<EnvimaniaBanResponse>, BadRequest<ValidationFailureResponse>, UnprocessableEntity<ActionUnprocessableResponse>>>(
            validResponse => TypedResults.Ok(validResponse),
            validationFailure => TypedResults.BadRequest(validationFailure),
            actionUnprocessable => TypedResults.UnprocessableEntity(actionUnprocessable)
        );
    }

    private static async Task<Results<Ok<EnvimaniaUnbanResponse>, BadRequest<ValidationFailureResponse>, UnprocessableEntity<ActionUnprocessableResponse>>> Unban(
        [FromBody] EnvimaniaUnbanRequest unbanRequest,
        IEnvimaniaService envimaniaService,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var result = await envimaniaService.UnbanAsync(unbanRequest, principal, cancellationToken);

        return result.Match<Results<Ok<EnvimaniaUnbanResponse>, BadRequest<ValidationFailureResponse>, UnprocessableEntity<ActionUnprocessableResponse>>>(
            validResponse => TypedResults.Ok(validResponse),
            validationFailure => TypedResults.BadRequest(validationFailure),
            actionUnprocessable => TypedResults.UnprocessableEntity(actionUnprocessable)
        );
    }

    private static async Task<Results<Ok<EnvimaniaSessionResponse>, BadRequest<ValidationFailureResponse>, ForbidHttpResult>> Session(
        [FromBody] EnvimaniaSessionRequest sessionRequest,
        IEnvimaniaService envimaniaService,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        if (!Validator.ValidateMapUid(sessionRequest.Map.Uid))
        {
            return TypedResults.BadRequest(new ValidationFailureResponse("Invalid MapUid"));
        }

        if (sessionRequest.Players.Length > 255)
        {
            return TypedResults.BadRequest(new ValidationFailureResponse("Too many players"));
        }

        var result = await envimaniaService.CreateSessionAsync(sessionRequest, cancellationToken);

        return result.Match<Results<Ok<EnvimaniaSessionResponse>, BadRequest<ValidationFailureResponse>, ForbidHttpResult>>(
            validResponse => TypedResults.Ok(validResponse),
            validationFailure => TypedResults.BadRequest(validationFailure),
            actionForbidden => TypedResults.Forbid()
        );
    }

    private static async Task<Results<Ok<EnvimaniaSessionStatusResponse>, ForbidHttpResult>> SessionStatus(
        IEnvimaniaService envimaniaService,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var result = await envimaniaService.CheckSessionStatusAsync(principal, cancellationToken);

        return result.Match<Results<Ok<EnvimaniaSessionStatusResponse>, ForbidHttpResult>>(
            validResponse => TypedResults.Ok(validResponse),
            actionForbidden => TypedResults.Forbid()
        );
    }

    private static async Task<Results<Ok<EnvimaniaSessionTokenResponse>, ForbidHttpResult>> SessionExtend(
        IEnvimaniaService envimaniaService,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var result = await envimaniaService.ExtendSessionAsync(principal, cancellationToken);

        return result.Match<Results<Ok<EnvimaniaSessionTokenResponse>, ForbidHttpResult>>(
            validResponse => TypedResults.Ok(validResponse),
            actionForbidden => TypedResults.Forbid()
        );
    }

    private static async Task<Results<Ok<EnvimaniaSessionRecordResponse>, BadRequest<ValidationFailureResponse>, ForbidHttpResult>> SessionRecord(
        [FromBody] EnvimaniaSessionRecordRequest sessionRecordRequest,
        IEnvimaniaService envimaniaService,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var result = await envimaniaService.SetSessionRecordAsync(sessionRecordRequest, context.User, context.Request, cancellationToken);

        return result.Match<Results<Ok<EnvimaniaSessionRecordResponse>, BadRequest<ValidationFailureResponse>, ForbidHttpResult>>(
            validResponse => TypedResults.Ok(validResponse),
            validationFailure => TypedResults.BadRequest(validationFailure),
            actionForbidden => TypedResults.Forbid()
        );
    }

    private static async Task<Results<Ok<EnvimaniaSessionRecordResponse>, BadRequest<ValidationFailureResponse>, ForbidHttpResult>> SessionRecordsPost(
        [FromBody] EnvimaniaSessionRecordBulkRequest sessionRecordBulkRequest,
        IEnvimaniaService envimaniaService,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var result = await envimaniaService.SetSessionRecordsAsync(sessionRecordBulkRequest, context.User, context.Request, cancellationToken);

        return result.Match<Results<Ok<EnvimaniaSessionRecordResponse>, BadRequest<ValidationFailureResponse>, ForbidHttpResult>>(
            validResponse => TypedResults.Ok(validResponse),
            validationFailure => TypedResults.BadRequest(validationFailure),
            actionForbidden => TypedResults.Forbid()
        );
    }

    private static async Task<Results<Ok<EnvimaniaRecordsResponse>, ForbidHttpResult>> SessionRecordsGet(
        string car,
        int? gravity,
        int? laps,
        IEnvimaniaService envimaniaService,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var filter = new EnvimaniaRecordFilter
        {
            Car = car,
            Gravity = gravity ?? 10,
            Laps = laps ?? 1
        };

        var result = await envimaniaService.GetSessionRecordsAsync(filter, context.User, context.Request, cancellationToken);

        return result.Match<Results<Ok<EnvimaniaRecordsResponse>, ForbidHttpResult>>(
            validResponse => TypedResults.Ok(validResponse),
            actionForbidden => TypedResults.Forbid()
        );
    }

    private static async Task<Results<Ok<EnvimaniaSessionClosedResponse>, ForbidHttpResult>> SessionClose(
        IEnvimaniaService envimaniaService,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var result = await envimaniaService.CloseSessionAsync(principal, cancellationToken);

        return result.Match<Results<Ok<EnvimaniaSessionClosedResponse>, ForbidHttpResult>>(
            validResponse => TypedResults.Ok(validResponse),
            actionForbidden => TypedResults.Forbid()
        );
    }

    private static async Task<Results<Ok<EnvimaniaRecordsResponse>, BadRequest<ValidationFailureResponse>>> Records(
        string mapUid,
        string car,
        int? gravity,
        int? laps,
        string? zone,
        IEnvimaniaService envimaniaService,
        HttpRequest httpRequest,
        CancellationToken cancellationToken)
    {
        var filter = new EnvimaniaRecordFilter
        {
            Car = car,
            Gravity = gravity ?? 10,
            Laps = laps ?? 1
        };

        var result = await envimaniaService.GetRecordsAsync(mapUid, filter, zone ?? "World", httpRequest, cancellationToken);

        return result.Match<Results<Ok<EnvimaniaRecordsResponse>, BadRequest<ValidationFailureResponse>>>(
            validResponse => TypedResults.Ok(validResponse),
            validationFailure => TypedResults.BadRequest(validationFailure)
        );
    }

    private static async Task<Results<Ok, BadRequest<ValidationFailureResponse>, ForbidHttpResult>> Record(
        HttpRequest request,
        IEnvimaniaService envimaniaService,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var result = await envimaniaService.SetRecordAsync(request, principal, cancellationToken);

        return result.Match<Results<Ok, BadRequest<ValidationFailureResponse>, ForbidHttpResult>>(
            validResponse => TypedResults.Ok(),
            validationFailure => TypedResults.BadRequest(validationFailure),
            actionForbidden => TypedResults.Forbid()
        );
    }

    private static async Task<Results<Ok<RatingServerResponse>, BadRequest<ValidationFailureResponse>, ForbidHttpResult>> SessionRate(
        [FromBody] RatingServerRequest[] request,
        IRatingService ratingService,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var result = await ratingService.SetAsync(request, principal, cancellationToken);

        return result.Match<Results<Ok<RatingServerResponse>, BadRequest<ValidationFailureResponse>, ForbidHttpResult>>(
            validResponse => TypedResults.Ok(validResponse),
            validationFailure => TypedResults.BadRequest(validationFailure),
            actionForbidden => TypedResults.Forbid()
        );
    }

    private static async Task<Results<Ok<EnvimaniaSessionUser>, ForbidHttpResult>> SessionUser(
        [FromBody] UserInfo request,
        IEnvimaniaService envimaniaService,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var result = await envimaniaService.GetSessionUserAdditionalInfoAsync(request, principal, cancellationToken);

        return result.Match<Results<Ok<EnvimaniaSessionUser>, ForbidHttpResult>>(
            validResponse => TypedResults.Ok(validResponse),
            actionForbidden => TypedResults.Forbid()
        );
    }

    private static async Task<Results<Ok<List<EnvimaniaSessionUser>>, BadRequest<ValidationFailureResponse>, ForbidHttpResult>> SessionUsers(
        [FromBody] Dictionary<string, UserInfo> request,
        IEnvimaniaService envimaniaService,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var result = await envimaniaService.GetSessionUsersAdditionalInfoAsync(request, principal, cancellationToken);

        return result.Match<Results<Ok<List<EnvimaniaSessionUser>>, BadRequest<ValidationFailureResponse>, ForbidHttpResult>>(
            validResponse => TypedResults.Ok(validResponse),
            validationFailure => TypedResults.BadRequest(validationFailure),
            actionForbidden => TypedResults.Forbid()
        );
    }

    private static readonly object restoreValidationsLock = new();
    private static Task? restoreValidationsTask;

    private static Results<Ok, Conflict> RestoreValidations(IServiceScopeFactory serviceScopeFactory, ILoggerFactory loggerFactory)
    {
        lock (restoreValidationsLock)
        {
            if (restoreValidationsTask is { IsCompleted: false })
            {
                return TypedResults.Conflict();
            }

            restoreValidationsTask = Task.Run(async () =>
            {
                try
                {
                    await using var scope = serviceScopeFactory.CreateAsyncScope();
                    await scope.ServiceProvider.GetRequiredService<IEnvimaniaService>().RestoreValidationsAsync(CancellationToken.None);
                }
                catch (Exception ex)
                {
                    loggerFactory.CreateLogger(typeof(EnvimaniaEndpoints).FullName!).LogError(ex, "Restoring validations failed");
                }
            }, CancellationToken.None);
        }

        return TypedResults.Ok();
    }

    private static readonly object restoreRecordsLock = new();
    private static Task? restoreRecordsTask;

    private static Results<Ok, Conflict> RestoreRecords(IServiceScopeFactory serviceScopeFactory, ILoggerFactory loggerFactory)
    {
        lock (restoreRecordsLock)
        {
            if (restoreRecordsTask is { IsCompleted: false })
            {
                return TypedResults.Conflict();
            }

            restoreRecordsTask = Task.Run(async () =>
            {
                try
                {
                    await using var scope = serviceScopeFactory.CreateAsyncScope();
                    await scope.ServiceProvider.GetRequiredService<IEnvimaniaService>().RestoreRecordsAsync(CancellationToken.None);
                }
                catch (Exception ex)
                {
                    loggerFactory.CreateLogger(typeof(EnvimaniaEndpoints).FullName!).LogError(ex, "Restoring records failed");
                }
            }, CancellationToken.None);
        }

        return TypedResults.Ok();
    }

    private static async Task<Results<Ok, BadRequest<ValidationFailureResponse>, ForbidHttpResult>> RemoveRecord(
        [FromBody] EnvimaniaRemoveRecordRequest removeRecordRequest,
        IEnvimaniaService envimaniaService,
        CancellationToken cancellationToken)
    {
        var result = await envimaniaService.RemoveRecordAsync(removeRecordRequest, cancellationToken);

        return result.Match<Results<Ok, BadRequest<ValidationFailureResponse>, ForbidHttpResult>>(
            validResponse => TypedResults.Ok(),
            validationFailure => TypedResults.BadRequest(validationFailure),
            actionForbidden => TypedResults.Forbid()
        );
    }

    private static async Task<Results<Ok, BadRequest<ValidationFailureResponse>, ForbidHttpResult>> RevertRecord(
        [FromBody] EnvimaniaRemoveRecordRequest revertRecordRequest,
        IEnvimaniaService envimaniaService,
        CancellationToken cancellationToken)
    {
        var result = await envimaniaService.RevertRecordAsync(revertRecordRequest, cancellationToken);
        return result.Match<Results<Ok, BadRequest<ValidationFailureResponse>, ForbidHttpResult>>(
            validResponse => TypedResults.Ok(),
            validationFailure => TypedResults.BadRequest(validationFailure),
            actionForbidden => TypedResults.Forbid()
        );
    }
}
