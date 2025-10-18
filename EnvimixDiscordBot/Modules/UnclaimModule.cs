using Discord;
using Discord.Interactions;
using EnvimixDiscordBot.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EnvimixDiscordBot.Modules;

public class UnclaimModule : InteractionModuleBase
{
    private readonly AppDbContext _db;
    private readonly DiscordReporter _discordReporter;
    private readonly IConfiguration _config;
    private readonly ILogger<UnclaimModule> _logger;

    public UnclaimModule(AppDbContext db, DiscordReporter discordReporter, IConfiguration config, ILogger<UnclaimModule> logger)
    {
        _db = db;
        _discordReporter = discordReporter;
        _config = config;
        _logger = logger;
    }

    [SlashCommand("unclaim", "Unclaim a map and a car from your list.")]
    public async Task Unclaim([Autocomplete(typeof(MapAutocompleteHandler))] string map)
    {
        using var _ = _logger.BeginScope("/unclaim {User}", Context.User.GlobalName);
        _logger.LogInformation("User {User} executed /unclaim", Context.User.GlobalName);

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

        if (convertedMap.ClaimedById is null)
        {
            _logger.LogDebug("Map {Map} has not been claimed.", map);
            await FollowupAsync("This map has not been claimed.", ephemeral: !IsBotChannel());
            return;
        }

        if ((await Context.Client.GetApplicationInfoAsync()).Owner.Id == user.Id)
        { 
            _logger.LogInformation("User {User} is the bot owner, skipping further checks.", user.GlobalName);
        }
        else
        {
            if (convertedMap.ClaimedById != user.Id)
            {
                _logger.LogInformation("User {User} did not claim map {Map}.", user.GlobalName, map);
                await FollowupAsync("This map has not been claimed by you.", ephemeral: !IsBotChannel());
                return;
            }

            if (convertedMap.Validated)
            {
                _logger.LogInformation("Map {Map} is already validated.", map);
                await FollowupAsync("This map is already validated and cannot be unclaimed.", ephemeral: !IsBotChannel());
                return;
            }
        }

        convertedMap.ClaimedById = null;
        convertedMap.ClaimedBy = null;

        _logger.LogInformation("Unclaiming...");

        await _db.SaveChangesAsync();

        _logger.LogInformation("Map {Map} unclaimed.", map);

        await _discordReporter.UpdateStatusDescriptionAsync(convertedMap.Campaign);

        await FollowupAsync($"Map '{convertedMap.Name}' unclaimed.", ephemeral: !IsBotChannel());
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
                .Where(x => !x.Validated && !x.Impossible && x.ClaimedById == context.User.Id && EF.Functions.Like(x.Name, $"%{map}%"))
                .Select(x => new { x.Name })
                .Take(25)
                .ToListAsync();

            return AutocompletionResult.FromSuccess(maps.Select(x => new AutocompleteResult(x.Name, x.Name)));
        }
    }
}