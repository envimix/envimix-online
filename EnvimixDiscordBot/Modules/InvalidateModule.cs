using Discord;
using Discord.Interactions;
using EnvimixDiscordBot.Models;
using EnvimixDiscordBot.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EnvimixDiscordBot.Modules;

public class InvalidateModule : InteractionModuleBase
{
    private readonly AppDbContext _db;
    private readonly DiscordReporter _discordReporter;
    private readonly IConfiguration _config;
    private readonly ILogger<InvalidateModule> _logger;

    public InvalidateModule(AppDbContext db, DiscordReporter discordReporter, IConfiguration config, ILogger<InvalidateModule> logger)
    {
        _db = db;
        _discordReporter = discordReporter;
        _config = config;
        _logger = logger;
    }

    [RequireOwner]
    [SlashCommand("invalidate", "Invalidate a map and a car.")]
    public async Task Invalidate([Autocomplete(typeof(MapAutocompleteHandler))] string map)
    {
        using var _ = _logger.BeginScope("/invalidate {User}", Context.User.GlobalName);
        _logger.LogInformation("User {User} executed /invalidate", Context.User.GlobalName);

        var user = Context.User;

        await DeferAsync(ephemeral: !IsBotChannel());

        var convertedMap = await _db.ConvertedMaps
            .Include(x => x.Campaign)
            .FirstOrDefaultAsync(x => x.Name == map);

        if (convertedMap is null)
        {
            _logger.LogDebug("Map {Map} does not exist.", map);
            await FollowupAsync("This map does not exist.", ephemeral: !IsBotChannel());
            return;
        }

        if (!convertedMap.Validated)
        {
            _logger.LogDebug("Map {Map} has not been validated.", map);
            await FollowupAsync("This map has not been validated.", ephemeral: !IsBotChannel());
            return;
        }

        convertedMap.Validated = false;
        convertedMap.Impossible = false;
        convertedMap.ClaimedById = null;
        convertedMap.ClaimedBy = null;

        _logger.LogInformation("Invalidating...");

        await _db.SaveChangesAsync();

        _logger.LogInformation("Map {Map} invalidated.", map);

        await _discordReporter.UpdateStatusDescriptionAsync(convertedMap.Campaign);

        await FollowupAsync($"Map '{convertedMap.Name}' invalidated.", ephemeral: !IsBotChannel());
    }

    private bool IsBotChannel()
    {
        var botChannelId = _config["TM2020:BotChannelId"];

        if (string.IsNullOrEmpty(botChannelId))
        {
            return false;
        }

        return Context.Channel.Id == ulong.Parse(botChannelId);
    }

    public class MapAutocompleteHandler : AutocompleteHandler
    {
        public override async Task<AutocompletionResult> GenerateSuggestionsAsync(
            IInteractionContext context,
            IAutocompleteInteraction autocompleteInteraction,
            IParameterInfo parameter,
            IServiceProvider services)
        {
            var map = autocompleteInteraction.Data.Current.Value.ToString() ?? "";

            await using var scope = services.CreateAsyncScope();

            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var maps = await db.ConvertedMaps
                .Where(x => x.Validated && EF.Functions.Like(x.Name, $"%{map}%"))
                .Select(x => new { x.Name })
                .Take(25)
                .ToListAsync();

            return AutocompletionResult.FromSuccess(maps.Select(x => new AutocompleteResult(x.Name, x.Name)));
        }
    }
}