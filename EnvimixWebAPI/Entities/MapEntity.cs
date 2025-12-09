using System.ComponentModel.DataAnnotations;

namespace EnvimixWebAPI.Entities;

public sealed class MapEntity
{
    [StringLength(34)]
    public required string Id { get; set; }

    [StringLength(255)]
    public string Name { get; set; } = "";

    public TitleEntity? TitlePack { get; set; }
    public string? TitlePackId { get; set; }

    public ServerEntity? FirstAppearedOnServer { get; set; }

    public bool IsCampaignMap { get; set; }
    public int? Order { get; set; }

    [StringLength(64)]
    public string Collection { get; set; } = "";

    // cycle issues when caching
    //public ICollection<EnvimaniaSessionEntity> EnvimaniaSessions { get; } = [];
    //public ICollection<RecordEntity> Records { get; } = [];
}
