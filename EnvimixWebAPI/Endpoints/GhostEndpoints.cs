using EnvimixWebAPI.Models;
using EnvimixWebAPI.Services;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using System.Text;
using TmEssentials;

namespace EnvimixWebAPI.Endpoints;

public class GhostEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        group.WithTags("Ghost");

        group.MapPost("submit/validable", SubmitGhost)
            .DisableAntiforgery();
        group.MapGet("{guid}/download", DownloadGhost);
    }

    private static async Task<IResult> SubmitGhost(
        HttpRequest request,
        IGhostSubmissionService ghostSubmissionService,
        CancellationToken cancellationToken)
    {
        if (!request.HasFormContentType)
        {
            return TypedResults.BadRequest("Expected multipart form data.");
        }

        var form = await request.ReadFormAsync(cancellationToken);
        var serverLogin = form["serverLogin"].ToString();
        var controllerCode = form["controllerCode"].ToString();
        var ghostFile = form.Files.GetFile("ghost");
        if (string.IsNullOrWhiteSpace(serverLogin)
            || string.IsNullOrWhiteSpace(controllerCode)
            || ghostFile is null
            || ghostFile.Length is 0 or > GhostSubmissionService.MaxGhostSize)
        {
            return TypedResults.BadRequest("Server login, controller code, and a valid ghost file are required.");
        }

        await using var ghostStream = ghostFile.OpenReadStream();
        var result = await ghostSubmissionService.SubmitAsync(
            ghostStream, serverLogin, controllerCode, cancellationToken);

        return result switch
        {
            GhostSubmissionResult.Attached => TypedResults.Ok(new { Status = "attached" }),
            GhostSubmissionResult.Queued => TypedResults.Accepted(uri: (string?)null, value: new { Status = "queued" }),
            GhostSubmissionResult.Unauthorized => TypedResults.Unauthorized(),
            GhostSubmissionResult.NotPersonalBest => TypedResults.UnprocessableEntity("The matching record is not the player's personal best."),
            GhostSubmissionResult.AlreadyAttached => TypedResults.Conflict("The matching record already has a ghost."),
            GhostSubmissionResult.QueueFull => TypedResults.StatusCode(StatusCodes.Status503ServiceUnavailable),
            _ => TypedResults.BadRequest("The uploaded file is not a valid ghost.")
        };
    }

    private static async Task<Results<FileContentHttpResult, NotFound>> DownloadGhost(Guid guid, AppDbContext db, HttpContext context, CancellationToken cancellationToken)
    {
        var record = await db.Records
            .Where(x => x.GhostId == guid || x.ValidationGhostId == guid)
            .Select(x => new
            {
                Data = x.GhostId == guid ? x.Ghost!.Data : x.ValidationGhost!.Data,
                LastModifiedAt = x.GhostId == guid ? x.Ghost!.LastModifiedAt : x.ValidationGhost!.LastModifiedAt,
                MapName = x.Map.Name,
                x.CarId,
                PlayerNickname = x.User.Nickname,
                PlayerLogin = x.UserId,
                x.Time
            })
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        if (record is null)
        {
            var ghost = await db.Ghosts
                .Where(x => x.Id == guid)
                .AsNoTracking()
                .FirstOrDefaultAsync(cancellationToken);

            if (ghost is null)
            {
                return TypedResults.NotFound();
            }

            return CreateGhostFile(ghost.Data, $"{guid}.Ghost.Gbx", ghost.LastModifiedAt, context);
        }

        var playerName = string.IsNullOrWhiteSpace(record.PlayerNickname) ? record.PlayerLogin : record.PlayerNickname;
        var fileName = $"{SanitizeFileName(record.MapName)}_{SanitizeFileName(record.CarId)}_{SanitizeFileName(playerName)}_({new TimeInt32(record.Time).ToString(useApostrophe: true)}).Ghost.Gbx";

        return CreateGhostFile(record.Data, fileName, record.LastModifiedAt, context);
    }

    private static FileContentHttpResult CreateGhostFile(byte[] data, string fileName, DateTimeOffset lastModifiedAt, HttpContext context)
    {
        // CORS middleware is ???
        if (context.Request.Headers.ContainsKey(CorsConstants.Origin))
        {
            context.Response.Headers.AccessControlAllowOrigin = "https://3d.gbx.tools";
            context.Response.Headers.AccessControlAllowMethods = "GET, OPTIONS";
            context.Response.Headers.AccessControlAllowHeaders = "*";
        }

        return TypedResults.File(data, "application/gbx", fileName, lastModified: lastModifiedAt);
    }

    private static string SanitizeFileName(string value)
    {
        var deformatted = TextFormatter.Deformat(value);
        var builder = new StringBuilder(deformatted.Length);
        var previousWasWhitespace = false;

        foreach (var character in deformatted)
        {
            var isInvalid = char.IsControl(character) || character is '<' or '>' or ':' or '"' or '/' or '\\' or '|' or '?' or '*';
            var output = isInvalid ? ' ' : character;
            if (char.IsWhiteSpace(output))
            {
                if (!previousWasWhitespace)
                {
                    builder.Append(' ');
                }
                previousWasWhitespace = true;
                continue;
            }

            builder.Append(output);
            previousWasWhitespace = false;
        }

        return builder.ToString().Trim(' ', '.');
    }
}
