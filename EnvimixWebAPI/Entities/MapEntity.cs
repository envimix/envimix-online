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
    public string? FirstAppearedOnServerId { get; set; }

    public bool IsCampaignMap { get; set; }
    public int? Order { get; set; }
    public int Laps { get; set; }

    [StringLength(64)]
    public string Collection { get; set; } = "";

    [StringLength(64)]
    public string? AuthorLogin { get; set; }

    [StringLength(255)]
    public string? AuthorNickname { get; set; }

    public CampaignEntity? Campaign { get; set; }
    public int? CampaignId { get; set; }

    public MapDataEntity? Data { get; set; }
    public int? DataId { get; set; }

    public int AuthorTime { get; set; }
    public int GoldTime { get; set; }
    public int SilverTime { get; set; }
    public int BronzeTime { get; set; }

    public CarEntity? DefaultCar { get; set; }
    public string? DefaultCarId { get; set; }

    public bool IsDefaultCar(string carId)
    {
        if (!string.IsNullOrWhiteSpace(DefaultCarId))
        {
            return DefaultCarId == carId;
        }

        return (Collection == "Canyon" && carId == "CanyonCar") ||
               (Collection == "Stadium" && carId == "StadiumCar") ||
               (Collection == "Valley" && carId == "ValleyCar") ||
               (Collection == "Lagoon" && carId == "LagoonCar");
    }

    // cycle issues when caching
    //public ICollection<EnvimaniaSessionEntity> EnvimaniaSessions { get; } = [];
    //public ICollection<RecordEntity> Records { get; } = [];
}
