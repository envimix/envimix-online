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

        group.MapGet("{guid}/download", DownloadGhost);
    }

    private static async Task<Results<FileContentHttpResult, NotFound>> DownloadGhost(Guid guid, AppDbContext db, HttpContext context, CancellationToken cancellationToken)
    {
        var record = await db.Records
            .Where(x => x.GhostId == guid)
            .Select(x => new
            {
                Data = x.Ghost!.Data,
                x.Ghost.LastModifiedAt,
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
