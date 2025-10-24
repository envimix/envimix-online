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
        return await db.Titles
            .Where(t => t.Id == titleId)
            .Select(t => new TitleReleaseInfo
            {
                ReleasedAt = t.ReleasedAt.HasValue ? t.ReleasedAt.Value.ToUnixTimeSeconds().ToString() : ""
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}
