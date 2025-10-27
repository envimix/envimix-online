using EnvimixWebAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace EnvimixWebAPI.Services;

public interface ITitleService
{
    Task<TitleReleaseInfo?> GetTitleReleaseInfoAsync(string titleId, CancellationToken cancellationToken);
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
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == titleId, cancellationToken);

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
}
