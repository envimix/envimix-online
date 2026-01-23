using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace EnvimixWebAPI.Entities;

[Index(nameof(Name))]
public sealed class CampaignEntity
{
    public int Id { get; set; }

    [StringLength(32)]
    public required string Name { get; set; }

    public TitleEntity? TitlePack { get; set; }
    public string? TitlePackId { get; set; }

    public DateTimeOffset? ReleasedAt { get; set; }
}
