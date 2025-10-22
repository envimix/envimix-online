using EnvimixWebsite.Authentication;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Security.Claims;

namespace EnvimixWebsite.Endpoints;

internal class PrivateDownloadEndpoint
{
    public static void Map(WebApplication app)
    {
        app.MapGet("/turbo/downloads/{fileName}", DownloadTurboFile)
            .RequireAuthorization(Policies.InsiderPolicy);
    }

    private static async Task<Results<PhysicalFileHttpResult, NotFound>> DownloadTurboFile(
        string fileName,
        ClaimsPrincipal principal,
        IWebHostEnvironment env, 
        ILogger<PrivateDownloadEndpoint> logger,
        CancellationToken cancellationToken)
    {
        var userName = principal.FindFirstValue(DiscordAdditionalClaims.GlobalName);

        logger.LogInformation("User {UserName} is downloading file {FileName}", userName, fileName);

        var fileInfo = env.ContentRootFileProvider.GetFileInfo(Path.Combine("PrivateDownloads", fileName));

        if (!fileInfo.Exists || fileInfo.IsDirectory || fileInfo.PhysicalPath is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.PhysicalFile(fileInfo.PhysicalPath, "application/octet-stream", fileInfo.Name, lastModified: fileInfo.LastModified, enableRangeProcessing: true);
    }
}
