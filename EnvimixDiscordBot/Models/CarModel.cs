using System.ComponentModel.DataAnnotations;

namespace EnvimixDiscordBot.Models;

public sealed class CarModel
{
    [StringLength(16)]
    public required string Id { get; set; }

    public override string ToString() => Id;
}
