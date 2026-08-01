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

    public CampaignEntity? Campaign { get; set; }
    public int? CampaignId { get; set; }

    public MapDataEntity? Data { get; set; }
    public int? DataId { get; set; }

    public int AuthorTime { get; set; }
    public int GoldTime { get; set; }
    public int SilverTime { get; set; }
    public int BronzeTime { get; set; }

    // cycle issues when caching
    //public ICollection<EnvimaniaSessionEntity> EnvimaniaSessions { get; } = [];
    //public ICollection<RecordEntity> Records { get; } = [];
}
