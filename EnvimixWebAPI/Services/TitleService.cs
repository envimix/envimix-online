using EnvimixWebAPI.Entities;
using EnvimixWebAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace EnvimixWebAPI.Services;

public interface ITitleService
{
    Task<TitleReleaseInfo?> GetTitleReleaseInfoAsync(string titleId, CancellationToken cancellationToken);
    Task<DateTimeOffset?> GetTitleReleaseDateAsync(string titleId, CancellationToken cancellationToken);
    Task<bool> SubmitTitleAsync(TitleSubmitRequest request, CancellationToken cancellationToken);
}

public sealed class TitleService : ITitleService
{
    private readonly AppDbContext db;

    public TitleService(AppDbContext db)
    {
        this.db = db;
    }

    public async Task<TitleReleaseInfo?> GetTitleReleaseInfoAsync(string titleId, CancellationToken cancellationToken)
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

        return new TitleReleaseInfo
        {
            ReleasedAt = title.ReleasedAt.HasValue ? title.ReleasedAt.Value.ToUnixTimeSeconds().ToString() : "",
            Key = title.ReleasedAt.HasValue && DateTimeOffset.UtcNow >= (title.ReleasedAt.Value - TimeSpan.FromSeconds(2)) ? (title.Key ?? "") : ""
        };
    }

    public async Task<DateTimeOffset?> GetTitleReleaseDateAsync(string titleId, CancellationToken cancellationToken)
    {
        return await db.Titles
            .Where(x => x.Id == titleId)
            .Select(x => x.ReleasedAt)
            .FirstOrDefaultAsync(cancellationToken);
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
