namespace EnvimixDiscordBot.Models;

public sealed class UserModel
{
    public ulong Id { get; set; }

    public ICollection<ConvertedMapModel> Maps { get; set; } = [];
}
