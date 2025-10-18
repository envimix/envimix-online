using System.ComponentModel.DataAnnotations;

namespace EnvimixWebAPI.Entities;

public sealed class CarEntity
{
    [StringLength(16)]
    public required string Id { get; set; }
}
