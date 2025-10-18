using System.ComponentModel.DataAnnotations;

namespace EnvimixWebAPI.Entities;

public sealed class ZoneEntity
{
    public int Id { get; set; }

    [StringLength(255)]
    public required string Name { get; set; }

    public ICollection<UserEntity> Users { get; } = [];
}
