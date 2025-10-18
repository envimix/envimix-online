using System.ComponentModel.DataAnnotations;

namespace EnvimixDiscordBot.Models;

public sealed class CampaignModel
{
    public int Id { get; set; }

    [StringLength(byte.MaxValue)]
    public required string Name { get; set; }

    public required UserModel? Submitter { get; set; }
    public required int? ClubId { get; set; }
    public ulong? NewsChannelId { get; set; }
    public ulong? NewsMessageId { get; set; }
    public ulong? StatusChannelId { get; set; }
    public ulong? StatusMessageId { get; set; }

    public ICollection<ConvertedMapModel> Maps { get; set; } = [];
}
