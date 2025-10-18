using Discord.Interactions;
using Microsoft.Extensions.Logging;

namespace EnvimixDiscordBot.Modules;

public class LegendModule : InteractionModuleBase
{
    private readonly ILogger<LegendModule> _logger;

    public LegendModule(ILogger<LegendModule> logger)
    {
        _logger = logger;
    }

    [SlashCommand("legend", "Explain each emoji used in validation status.")]
    public async Task Help()
    {
        using var _ = _logger.BeginScope("/legend {User}", Context.User.GlobalName);
        _logger.LogInformation("User {User} executed /legend", Context.User.GlobalName);

        await RespondAsync("🟦 - Unclaimed - combination can be claimed by anyone\n🟧 - Claimed - combination is *planned* to be validated by someone\n✅ - Validated - combination is validated\n❌ - Impossible - combination is *claimed* impossible\n✖️ - Invalid - combination does not exist", ephemeral: true);
    }
}