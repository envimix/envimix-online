using EnvimixWebAPI.Entities;
using EnvimixWebAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using System.Security.Claims;

namespace EnvimixWebAPI.Services;

public interface ITitleService
{
    Task<TitleReleaseInfo?> GetTitleReleaseInfoAsync(string titleId, ClaimsPrincipal principal, CancellationToken cancellationToken);
    Task<DateTimeOffset?> GetTitleReleaseDateAsync(string titleId, CancellationToken cancellationToken);
    Task<bool> SubmitTitleAsync(TitleSubmitRequest request, CancellationToken cancellationToken);
}

public sealed class TitleService : ITitleService
{
    private readonly AppDbContext db;
    private readonly HybridCache cache;

    public TitleService(AppDbContext db, HybridCache cache)
    {
        this.db = db;
        this.cache = cache;
    }

    public async Task<TitleReleaseInfo?> GetTitleReleaseInfoAsync(string titleId, ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var title = await db.Titles
            .Where(x => x.Id == titleId)
            .Select(x => new
            {
                x.ReleasedAt,
                x.Key
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (title is null)
        {
            return null;
        }

        var releasedAt = title.ReleasedAt;

        // for admins, allow access to title pack immediately
        /*if (releasedAt.HasValue && principal.IsInRole(Roles.Admin))
        {
            var adminReleaseDate = DateTimeOffset.UtcNow - TimeSpan.FromMinutes(5);
            releasedAt = releasedAt.Value > adminReleaseDate ? adminReleaseDate : releasedAt.Value;
        }*/

        return new TitleReleaseInfo
        {
            ReleasedAt = releasedAt.HasValue ? releasedAt.Value.ToUnixTimeSeconds().ToString() : "",
            Key = releasedAt.HasValue && DateTimeOffset.UtcNow >= (releasedAt.Value - TimeSpan.FromSeconds(2)) ? (title.Key ?? "") : ""
        };
    }

    public async Task<DateTimeOffset?> GetTitleReleaseDateAsync(string titleId, CancellationToken cancellationToken)
    {
        return await cache.GetOrCreateAsync($"TitleReleaseDate_{titleId}", async token =>
        {
            return await db.Titles
                .Where(x => x.Id == titleId)
                .Select(x => x.ReleasedAt)
                .FirstOrDefaultAsync(token);
        }, new() { Expiration = TimeSpan.FromHours(1) }, cancellationToken: cancellationToken);
    }

    public async Task<bool> SubmitTitleAsync(TitleSubmitRequest request, CancellationToken cancellationToken)
    {
        var title = await db.Titles.FirstOrDefaultAsync(t => t.Id == request.TitleId, cancellationToken);

        if (title is null)
        {
            title = new TitleEntity
            {
                Id = request.TitleId,
                ReleasedAt = DateTimeOffset.UtcNow
            };
            await db.Titles.AddAsync(title, cancellationToken);
        }

        title.DisplayName = request.Name;
        title.Version = request.Version;

        return await db.SaveChangesAsync(cancellationToken) > 0;
    }
}
