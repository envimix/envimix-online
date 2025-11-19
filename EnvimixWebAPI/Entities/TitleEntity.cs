using System.ComponentModel.DataAnnotations;

namespace EnvimixWebAPI.Entities;

public sealed class TitleEntity
{
    [StringLength(64)]
    public required string Id { get; set; }

    [StringLength(255)]
    public string? DisplayName { get; set; }

    public required DateTimeOffset? ReleasedAt { get; set; }

    [StringLength(64)]
    public string? Key { get; set; }

    [StringLength(byte.MaxValue)]
    public string? Version { get; set; }
}
