using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

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
        var ghost = await db.Ghosts
            .Where(x => x.Id == guid)
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        if (ghost is null)
        {
            return TypedResults.NotFound();
        }

        // CORS middleware is ???
        if (context.Request.Headers.ContainsKey(CorsConstants.Origin))
        {
            context.Response.Headers.AccessControlAllowOrigin = "https://3d.gbx.tools";
            context.Response.Headers.AccessControlAllowMethods = "GET, OPTIONS";
            context.Response.Headers.AccessControlAllowHeaders = "*";
        }

        return TypedResults.File(ghost.Data, "application/gbx", $"{guid}.Ghost.Gbx", lastModified: ghost.LastModifiedAt);
    }
}
