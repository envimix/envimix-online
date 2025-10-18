using Discord.Interactions;
using Microsoft.Extensions.Logging;

namespace EnvimixDiscordBot.Modules;

public class HelpModule : InteractionModuleBase
{
    private readonly ILogger<HelpModule> _logger;

    public HelpModule(ILogger<HelpModule> logger)
    {
        _logger = logger;
    }

    [SlashCommand("help", "Help about how to validate envimix maps.")]
    public async Task Help()
    {
        using var _ = _logger.BeginScope("/help {User}", Context.User.GlobalName);
        _logger.LogInformation("User {User} executed /help", Context.User.GlobalName);

        await RespondAsync("**ENVIMIX bot** is a larger project about a challenge invented by Poutrel by changing cars on existing maps and trying to validate them.\n\nFor TM2020, I have developed a bot that can automatically give and gather us the maps to use in the Envimix club.\n\nFirst, to validate a combination, use `/claim map:[Your Map] - [Car]`, autocomplete will help you (only valid maps are suggested). Map will be given out, or just download the map pack from the news channel.\n\nAfter claiming, validating the map, and **calculating the shadows** in the editor, you can submit your validated maps with the `/validate` command. You can also zip the maps (only .zip is supported).\n\nIf the track is too hard for you, you can either `/unclaim` it to allow someone else to try it, or if you think it's impossible, use the `/impossible` command (allowed **1 hour after claiming**). Impossible tracks can be validated by anyone else without claiming.\n\nDo not set fake times. If caught, you won't be allowed to validate further.", ephemeral: true);
    }
}