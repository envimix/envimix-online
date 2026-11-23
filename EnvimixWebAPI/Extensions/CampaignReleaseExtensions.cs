using EnvimixWebAPI.Entities;

namespace EnvimixWebAPI.Extensions;

/// <summary>
/// Visibility rules for campaigns on public (website-facing) endpoints.
/// A named campaign is hidden until it has a release date that has passed. A campaign is expected
/// to have no release date right after it is first submitted, so a missing date means unreleased.
/// The main campaign (empty name) and maps without a campaign follow the title pack release instead.
/// </summary>
public static class CampaignReleaseExtensions
{
    public static bool IsReleased(this CampaignEntity? campaign, DateTimeOffset now)
        => campaign is null
        || campaign.Name == ""
        || (campaign.ReleasedAt != null && campaign.ReleasedAt <= now);

    public static IQueryable<MapEntity> WhereCampaignReleased(this IQueryable<MapEntity> maps, DateTimeOffset now)
        => maps.Where(x => x.Campaign == null
            || x.Campaign.Name == ""
            || (x.Campaign.ReleasedAt != null && x.Campaign.ReleasedAt <= now));

    public static IQueryable<RecordEntity> WhereCampaignReleased(this IQueryable<RecordEntity> records, DateTimeOffset now)
        => records.Where(x => x.Map.Campaign == null
            || x.Map.Campaign.Name == ""
            || (x.Map.Campaign.ReleasedAt != null && x.Map.Campaign.ReleasedAt <= now));

    public static IQueryable<EnvimaniaSessionEntity> WhereCampaignReleased(this IQueryable<EnvimaniaSessionEntity> sessions, DateTimeOffset now)
        => sessions.Where(x => x.Map.Campaign == null
            || x.Map.Campaign.Name == ""
            || (x.Map.Campaign.ReleasedAt != null && x.Map.Campaign.ReleasedAt <= now));
}
