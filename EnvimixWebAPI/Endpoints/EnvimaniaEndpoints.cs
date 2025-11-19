using EnvimixWebAPI.Models;
using EnvimixWebAPI.Models.Envimania;
using EnvimixWebAPI.Services;
using Microsoft.AspNetCore.Http.HttpResults;
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

        group.MapPost("record", Record).RequireAuthorization(Policies.ManiaPlanetUserPolicy);

        MapSession(group.MapGroup("session"));
    }

    private static void MapSession(RouteGroupBuilder group)
    {
        group.MapPost("", Session).RequireRateLimiting("20PerHour");
        group.MapGet("status", SessionStatus).RequireAuthorization(Policies.EnvimaniaSessionPolicy);
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
        IEnvimaniaService envimaniaService,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var result = await envimaniaService.RegisterAsync(registerRequest, principal, cancellationToken);

        return result.Match<Results<Ok<EnvimaniaServer>, BadRequest<ValidationFailureResponse>, UnprocessableEntity<ActionUnprocessableResponse>, ForbidHttpResult>>(
            validResponse => TypedResults.Ok(validResponse), // TODO: use Created here instead
            validationFailure => TypedResults.BadRequest(validationFailure),
            actionUnprocessable => TypedResults.UnprocessableEntity(actionUnprocessable),
            actionForbidden => TypedResults.Forbid()
        );
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

    private static async Task<Results<Ok<EnvimaniaSessionRecordResponse>, BadRequest<ValidationFailureResponse>, ForbidHttpResult>> SessionRecord(
        [FromBody] EnvimaniaSessionRecordRequest sessionRecordRequest,
        IEnvimaniaService envimaniaService,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var result = await envimaniaService.SetSessionRecordAsync(sessionRecordRequest, principal, cancellationToken);

        return result.Match<Results<Ok<EnvimaniaSessionRecordResponse>, BadRequest<ValidationFailureResponse>, ForbidHttpResult>>(
            validResponse => TypedResults.Ok(validResponse),
            validationFailure => TypedResults.BadRequest(validationFailure),
            actionForbidden => TypedResults.Forbid()
        );
    }

    private static async Task<Results<Ok<EnvimaniaSessionRecordResponse>, BadRequest<ValidationFailureResponse>, ForbidHttpResult>> SessionRecordsPost(
        [FromBody] EnvimaniaSessionRecordBulkRequest sessionRecordBulkRequest,
        IEnvimaniaService envimaniaService,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var result = await envimaniaService.SetSessionRecordsAsync(sessionRecordBulkRequest, principal, cancellationToken);

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
        CancellationToken cancellationToken)
    {
        var filter = new EnvimaniaRecordFilter
        {
            Car = car,
            Gravity = gravity ?? 10,
            Laps = laps ?? 1
        };

        var result = await envimaniaService.GetRecordsAsync(mapUid, filter, zone ?? "World", cancellationToken);

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
}
