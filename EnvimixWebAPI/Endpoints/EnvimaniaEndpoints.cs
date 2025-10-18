using EnvimixWebAPI.Models;
using EnvimixWebAPI.Models.Envimania;
using EnvimixWebAPI.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EnvimixWebAPI.Endpoints;

public static class EnvimaniaEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        group.WithTags("Envimania");

        group.MapPost("register", Register)
            .RequireAuthorization(Policies.ManiaPlanetUserPolicy);

        group.MapPost("ban", Ban).RequireAuthorization(Policies.SuperAdminPolicy);
        group.MapPost("unban", Unban).RequireAuthorization(Policies.SuperAdminPolicy);
        group.MapGet("records/{mapUid}/{car}", Records);

        MapSession(group.MapGroup("session"));
    }

    private static void MapSession(RouteGroupBuilder group)
    {
        group.RequireAuthorization(Policies.ManiaPlanetRegisteredServerPolicy);

        group.MapPost("", Session);
        group.MapGet("status", SessionStatus);
        group.MapPost("record", SessionRecord);
        group.MapPost("records", SessionRecordsPost);
        group.MapGet("records/{car}", SessionRecordsGet);
        group.MapPost("rate", SessionRate);
        group.MapPost("user", SessionUser);
        group.MapPost("users", SessionUsers);
        group.MapPost("close", SessionClose);
    }

    private static async Task<IResult> Register(
        [FromBody] EnvimaniaRegistrationRequest registerRequest,
        IEnvimaniaService envimaniaService,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var result = await envimaniaService.RegisterAsync(registerRequest, principal, cancellationToken);

        return result.Match<IResult>(
            validResponse => TypedResults.Ok(validResponse), // TODO: use Created here instead
            validationFailure => TypedResults.BadRequest(validationFailure),
            actionUnprocessable => TypedResults.UnprocessableEntity(actionUnprocessable),
            actionForbidden => TypedResults.Forbid()
        );
    }

    private static async Task<IResult> Ban(
        [FromBody] EnvimaniaBanRequest banRequest,
        IEnvimaniaService envimaniaService,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var result = await envimaniaService.BanAsync(banRequest, principal, cancellationToken);

        return result.Match<IResult>(
            validResponse => TypedResults.Ok(validResponse),
            validationFailure => TypedResults.BadRequest(validationFailure),
            actionUnprocessable => TypedResults.UnprocessableEntity(actionUnprocessable)
        );
    }

    private static async Task<IResult> Unban(
        [FromBody] EnvimaniaUnbanRequest unbanRequest,
        IEnvimaniaService envimaniaService,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var result = await envimaniaService.UnbanAsync(unbanRequest, principal, cancellationToken);

        return result.Match<IResult>(
            validResponse => TypedResults.Ok(validResponse),
            validationFailure => TypedResults.BadRequest(validationFailure),
            actionUnprocessable => TypedResults.UnprocessableEntity(actionUnprocessable)
        );
    }

    private static async Task<IResult> Session(
        [FromBody] EnvimaniaSessionRequest sessionRequest,
        IEnvimaniaService envimaniaService,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var result = await envimaniaService.CreateSessionAsync(sessionRequest, principal, cancellationToken);

        return result.Match<IResult>(
            validResponse => TypedResults.Ok(validResponse),
            validationFailure => TypedResults.BadRequest(validationFailure),
            actionForbidden => TypedResults.Forbid()
        );
    }

    private static async Task<IResult> SessionStatus(
        IEnvimaniaService envimaniaService,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var result = await envimaniaService.CheckSessionStatusAsync(principal, cancellationToken);

        return result.Match<IResult>(
            validResponse => TypedResults.Ok(validResponse),
            actionForbidden => TypedResults.Forbid()
        );
    }

    private static async Task<IResult> SessionRecord(
        [FromBody] EnvimaniaSessionRecordRequest sessionRecordRequest,
        IEnvimaniaService envimaniaService,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var result = await envimaniaService.SetSessionRecordAsync(sessionRecordRequest, principal, cancellationToken);

        return result.Match<IResult>(
            validResponse => TypedResults.Ok(validResponse),
            validationFailure => TypedResults.BadRequest(validationFailure),
            actionForbidden => TypedResults.Forbid()
        );
    }

    private static async Task<IResult> SessionRecordsPost(
        [FromBody] EnvimaniaSessionRecordBulkRequest sessionRecordBulkRequest,
        IEnvimaniaService envimaniaService,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var result = await envimaniaService.SetSessionRecordsAsync(sessionRecordBulkRequest, principal, cancellationToken);

        return result.Match<IResult>(
            validResponse => TypedResults.Ok(validResponse),
            validationFailure => TypedResults.BadRequest(validationFailure),
            actionForbidden => TypedResults.Forbid()
        );
    }

    private static async Task<IResult> SessionRecordsGet(
        string car,
        int? gravity,
        int? laps,
        IEnvimaniaService envimaniaService,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var filter = new EnvimaniaRecordFilter
        {
            Car = car,
            Gravity = gravity ?? 10,
            Laps = laps ?? 1
        };

        var result = await envimaniaService.GetSessionRecordsAsync(filter, principal, cancellationToken);

        return result.Match<IResult>(
            validResponse => TypedResults.Ok(validResponse),
            actionForbidden => TypedResults.Forbid()
        );
    }

    private static async Task<IResult> SessionClose(
        IEnvimaniaService envimaniaService,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var result = await envimaniaService.CloseSessionAsync(principal, cancellationToken);

        return result.Match<IResult>(
            validResponse => TypedResults.Ok(validResponse),
            actionForbidden => TypedResults.Forbid()
        );
    }

    private static async Task<IResult> Records(
        string mapUid,
        string car,
        int? gravity,
        int? laps,
        string? zone,
        IEnvimaniaService envimaniaService,
        CancellationToken cancellationToken)
    {
        var filter = new EnvimaniaRecordFilter
        {
            Car = car,
            Gravity = gravity ?? 10,
            Laps = laps ?? 1
        };

        var result = await envimaniaService.GetRecordsAsync(mapUid, filter, zone ?? "World", cancellationToken);

        return result.Match<IResult>(
            validResponse => TypedResults.Ok(validResponse),
            validationFailure => TypedResults.BadRequest(validationFailure)
        );
    }

    private static async Task<IResult> SessionRate(
        [FromBody] RatingServerRequest[] request,
        IRatingService ratingService,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var result = await ratingService.SetAsync(request, principal, cancellationToken);

        return result.Match<IResult>(
            validResponse => TypedResults.Ok(validResponse),
            validationFailure => TypedResults.BadRequest(validationFailure),
            actionForbidden => TypedResults.Forbid()
        );
    }

    private static async Task<IResult> SessionUser(
        [FromBody] UserInfo request,
        IEnvimaniaService envimaniaService,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var result = await envimaniaService.GetSessionUserAdditionalInfoAsync(request, principal, cancellationToken);

        return result.Match<IResult>(
            validResponse => TypedResults.Ok(validResponse),
            actionForbidden => TypedResults.Forbid()
        );
    }

    private static async Task<IResult> SessionUsers(
        [FromBody] Dictionary<string, UserInfo> request,
        IEnvimaniaService envimaniaService,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var result = await envimaniaService.GetSessionUsersAdditionalInfoAsync(request, principal, cancellationToken);

        return result.Match<IResult>(
            validResponse => TypedResults.Ok(validResponse),
            validationFailure => TypedResults.BadRequest(validationFailure),
            actionForbidden => TypedResults.Forbid()
        );
    }
}
