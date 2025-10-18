using Discord;
using Discord.Interactions;
using EnvimixDiscordBot.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EnvimixDiscordBot.Modules;

public class ImpossibleModule : InteractionModuleBase
{
    private readonly AppDbContext _db;
    private readonly DiscordReporter _discordReporter;
    private readonly IConfiguration _config;
    private readonly ILogger<UnclaimModule> _logger;

    public ImpossibleModule(AppDbContext db, DiscordReporter discordReporter, IConfiguration config, ILogger<UnclaimModule> logger)
    {
        _db = db;
        _discordReporter = discordReporter;
        _config = config;
        _logger = logger;
    }

    [SlashCommand("impossible", "Set a claimed map and a car as impossible.")]
    public async Task Impossible([Autocomplete(typeof(MapAutocompleteHandler))] string map)
    {
        using var _ = _logger.BeginScope("/impossible {User}", Context.User.GlobalName);
        _logger.LogInformation("User {User} executed /impossible", Context.User.GlobalName);

        var user = Context.User;

        var convertedMap = await _db.ConvertedMaps
            .Include(x => x.Campaign)
            .Include(x => x.Campaign!.Maps)
            .FirstOrDefaultAsync(x => x.Name == map);

        if (convertedMap is null)
        {
            _logger.LogDebug("Map {Map} does not exist.", map);
            await RespondAsync("This map does not exist.", ephemeral: true);
            return;
        }

        if ((await Context.Client.GetApplicationInfoAsync()).Owner.Id == user.Id)
        { 
            _logger.LogInformation("User {User} is the bot owner, skipping further checks.", user.GlobalName);
        }
        else
        {
            if (convertedMap.ClaimedById is null)
            {
                _logger.LogDebug("Map {Map} has not been claimed.", map);
                await RespondAsync("This map has not been claimed.", ephemeral: true);
                return;
            }

            if (convertedMap.ClaimedById != user.Id)
            {
                _logger.LogInformation("User {User} did not claim map {Map}.", user.GlobalName, map);
                await RespondAsync("This map has not been claimed by you.", ephemeral: true);
                return;
            }

            if (convertedMap.Validated)
            {
                _logger.LogInformation("Map {Map} is already validated.", map);
                await RespondAsync("This map is already validated and cannot be unclaimed.", ephemeral: true);
                return;
            }

            if (convertedMap.ClaimedAt.HasValue && DateTimeOffset.Now - convertedMap.ClaimedAt.Value < TimeSpan.FromHours(1))
            {
                _logger.LogInformation("Map {Map} was claimed less than an hour ago.", map);
                await RespondAsync($"This map can be set as impossible {TimestampTag.FormatFromDateTimeOffset(convertedMap.ClaimedAt.Value + TimeSpan.FromHours(1), TimestampTagStyles.Relative)}.", ephemeral: true);
                return;
            }
        }

        _logger.LogInformation("Setting map {Map} as impossible.", map);

        convertedMap.Impossible = true;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Map {Map} set as impossible.", map);

        await RespondAsync($"Map '{convertedMap.Name}' set as impossible.", ephemeral: !IsBotChannel());

        await _discordReporter.UpdateStatusDescriptionAsync(convertedMap.Campaign);
        await _discordReporter.CheckCampaignCompletionAsync(convertedMap.Campaign, convertedMap.CarId);
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