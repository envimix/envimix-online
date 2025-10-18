using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EnvimixDiscordBot.Models;

[Index(nameof(Name), IsUnique = true)]
[Index(nameof(OriginalUid))]
public sealed class ConvertedMapModel
{
    public int Id { get; set; }

    /// <summary>
    /// Full map name (as visible ingame), including the car name.
    /// </summary>
    [StringLength(64)]
    public required string Name { get; set; }

    public string CarId { get; set; } = default!;
    public required CarModel Car { get; set; }

    [StringLength(36)]
    public required string Uid { get; set; }

    [StringLength(byte.MaxValue)]
    public required string OriginalName { get; set; }

    [StringLength(36)]
    public required string OriginalUid { get; set; }

    [Column(TypeName = "mediumblob")]
    public required byte[] Data { get; set; }

    public bool Validated { get; set; }
    public bool Impossible { get; set; }
    public int Order { get; set; }

    public ulong? ClaimedById { get; set; }
    public UserModel? ClaimedBy { get; set; }
    public DateTimeOffset? ClaimedAt { get; set; }

    public DateTimeOffset? LastModifiedAt { get; set; }

    public int CampaignId { get; set; }
    public required CampaignModel Campaign { get; set; }

    public string GetFileName() => $"{OriginalName} - {CarId}.Map.Gbx";
}
