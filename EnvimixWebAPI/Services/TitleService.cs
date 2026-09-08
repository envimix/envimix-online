using EnvimixWebAPI.Entities;
using EnvimixWebAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using ManiaAPI.ManiaPlanetAPI;
using System.Diagnostics;
using System.Security.Claims;

namespace EnvimixWebAPI.Services;

public interface ITitleService
{
    Task<TitleReleaseInfo?> GetTitleReleaseInfoAsync(string titleId, ClaimsPrincipal principal, CancellationToken cancellationToken);
    Task<DateTimeOffset?> GetTitleReleaseDateAsync(string titleId, CancellationToken cancellationToken);
    Task<Dictionary<string, DateTimeOffset>> GetCampaignReleaseDatesAsync(string titleId, CancellationToken cancellationToken);
    Task<bool> SubmitTitleAsync(TitleSubmitRequest request, CancellationToken cancellationToken);
    Task<bool> RegisterTitleAsync(TitleRegistrationRequest request, CancellationToken cancellationToken);
}

public sealed class TitleService(AppDbContext db, HybridCache cache, ManiaPlanetIngameAPI mpIngameApi) : ITitleService
{
    private static readonly ActivitySource ActivitySource = new("EnvimixWebAPI.Services.TitleService");

    public async Task<TitleReleaseInfo?> GetTitleReleaseInfoAsync(string titleId, ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var title = await db.Titles
            .Include(x => x.Campaigns)
            .Where(x => x.Id == titleId)
            .Select(x => new
            {
                x.Campaigns,
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

        var campaignsReleasedAt = title.Campaigns
            .Where(x => x.Name != "")
            .ToDictionary(
                x => x.Name,
                x => x.ReleasedAt.HasValue ? x.ReleasedAt.Value.ToUnixTimeSeconds().ToString() : ""
            );

        return new TitleReleaseInfo
        {
            ReleasedAt = releasedAt.HasValue ? releasedAt.Value.ToUnixTimeSeconds().ToString() : "",
            Key = releasedAt.HasValue && DateTimeOffset.UtcNow >= (releasedAt.Value - TimeSpan.FromSeconds(2)) ? (title.Key ?? "") : "",
            CampaignsReleasedAt = campaignsReleasedAt
        };
    }

    public async Task<DateTimeOffset?> GetTitleReleaseDateAsync(string titleId, CancellationToken cancellationToken)
    {
        using var activity = ActivitySource.StartActivity(nameof(GetTitleReleaseDateAsync));
        activity?.SetTag("titleId", titleId);

        return await cache.GetOrCreateAsync($"TitleReleaseDate_{titleId}", async token =>
        {
            return await db.Titles
                .Where(x => x.Id == titleId)
                .Select(x => x.ReleasedAt)
                .FirstOrDefaultAsync(token);
        }, new() { Expiration = TimeSpan.FromHours(1) }, cancellationToken: cancellationToken);
    }

    public async Task<Dictionary<string, DateTimeOffset>> GetCampaignReleaseDatesAsync(string titleId, CancellationToken cancellationToken)
    {
        using var activity = ActivitySource.StartActivity(nameof(GetCampaignReleaseDatesAsync));
        activity?.SetTag("titleId", titleId);
        return await cache.GetOrCreateAsync($"CampaignReleaseDates_{titleId}", async token =>
        {
            return await db.Campaigns
                .Where(x => x.TitlePackId == titleId && x.Name != "" && x.ReleasedAt.HasValue)
                .ToDictionaryAsync(x => x.Name, x => x.ReleasedAt!.Value, token);
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

    public async Task<bool> RegisterTitleAsync(TitleRegistrationRequest request, CancellationToken cancellationToken)
    {
        var titleInfo = await mpIngameApi.GetTitleByUidAsync(request.TitleId, cancellationToken);
        
        if (titleInfo is null)
        {
            return false;
        }
        
        var title = await db.Titles.FirstOrDefaultAsync(t => t.Id == titleInfo.Uid, cancellationToken);

        if (title is null)
        {
            title = new TitleEntity
            {
                Id = titleInfo.Uid,
                ReleasedAt =  null
            };
            await db.Titles.AddAsync(title, cancellationToken);
        }

        title.DisplayName = titleInfo.Name;

        return await db.SaveChangesAsync(cancellationToken) > 0;
    }
}
