using EnvimixWebAPI.Models;
using EnvimixWebAPI.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace EnvimixWebAPI.Endpoints;

public static class ReplayEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        group.WithTags("Replay");

        group.MapPost("submit", SubmitReplay)
            .DisableAntiforgery();
        group.MapPost("submit/validable", SubmitValidationReplay)
            .DisableAntiforgery();
        group.MapGet("{guid:guid}/download", DownloadReplay);
    }

    private static async Task<IResult> SubmitReplay(
        HttpRequest request,
        IReplaySubmissionService replaySubmissionService,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
        => await SubmitReplayAsync(
            request,
            replaySubmissionService,
            loggerFactory.CreateLogger("EnvimixWebAPI.Endpoints.ReplayEndpoints"),
            isValidation: false,
            cancellationToken);

    private static async Task<IResult> SubmitValidationReplay(
        HttpRequest request,
        IReplaySubmissionService replaySubmissionService,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
        => await SubmitReplayAsync(
            request,
            replaySubmissionService,
            loggerFactory.CreateLogger("EnvimixWebAPI.Endpoints.ReplayEndpoints"),
            isValidation: true,
            cancellationToken);

    private static async Task<IResult> SubmitReplayAsync(
        HttpRequest request,
        IReplaySubmissionService replaySubmissionService,
        ILogger logger,
        bool isValidation,
        CancellationToken cancellationToken)
    {
        if (!request.HasFormContentType)
        {
            logger.LogWarning("Rejected {ReplayKind} replay submission because it was not multipart form data.", ReplayKind(isValidation));
            return TypedResults.BadRequest("Expected multipart form data.");
        }

        var form = await request.ReadFormAsync(cancellationToken);
        var serverLogin = form["serverLogin"].ToString();
        var controllerCode = form["controllerCode"].ToString();
        var replayFile = form.Files.GetFile("replay");
        if (string.IsNullOrWhiteSpace(serverLogin)
            || string.IsNullOrWhiteSpace(controllerCode)
            || replayFile is null
            || replayFile.Length is 0 or > ReplaySubmissionService.MaxReplaySize)
        {
            logger.LogWarning("Rejected {ReplayKind} replay submission from server {ServerLogin} because required form data or replay file was invalid.", ReplayKind(isValidation), serverLogin);
            return TypedResults.BadRequest("Server login, controller code, and a valid replay file are required.");
        }

        await using var replayStream = replayFile.OpenReadStream();
        var result = await replaySubmissionService.SubmitAsync(
            replayStream, serverLogin, controllerCode, isValidation, cancellationToken);

        logger.LogInformation(
            "{ReplayKind} replay submission from server {ServerLogin} completed with result {Result}.",
            ReplayKind(isValidation),
            serverLogin,
            result);
        return result switch
        {
            ReplaySubmissionResult.Attached => TypedResults.Ok(new { Status = "attached" }),
            ReplaySubmissionResult.Queued => TypedResults.Accepted(uri: (string?)null, value: new { Status = "queued" }),
            ReplaySubmissionResult.Unauthorized => TypedResults.Unauthorized(),
            ReplaySubmissionResult.NotPersonalBest => TypedResults.UnprocessableEntity("The matching record is not the player's personal best."),
            ReplaySubmissionResult.AlreadyAttached => TypedResults.Conflict($"The matching record already has a {(isValidation ? "validation " : "")}replay."),
            ReplaySubmissionResult.QueueFull => TypedResults.StatusCode(StatusCodes.Status503ServiceUnavailable),
            _ => TypedResults.BadRequest("The uploaded file is not a valid replay.")
        };
    }

    private static string ReplayKind(bool isValidation) => isValidation ? "validation" : "main";

    private static async Task<Results<FileContentHttpResult, NotFound>> DownloadReplay(
        Guid guid,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var replay = await db.Replays
            .Where(x => x.Id == guid)
            .Select(x => new { x.Data, x.LastModifiedAt })
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        return replay is null
            ? TypedResults.NotFound()
            : TypedResults.File(
                replay.Data,
                "application/gbx",
                $"{guid}.Replay.Gbx",
                lastModified: replay.LastModifiedAt);
    }
}
