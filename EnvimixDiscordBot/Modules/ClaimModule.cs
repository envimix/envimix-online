using Discord;
using Discord.Interactions;
using EnvimixDiscordBot.Models;
using EnvimixDiscordBot.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EnvimixDiscordBot.Modules;

public class ClaimModule : InteractionModuleBase
{
    private readonly AppDbContext _db;
    private readonly DiscordReporter _discordReporter;
    private readonly IConfiguration _config;
    private readonly ILogger<ClaimModule> _logger;

    public ClaimModule(AppDbContext db, DiscordReporter discordReporter, IConfiguration config, ILogger<ClaimModule> logger)
    {
        _db = db;
        _discordReporter = discordReporter;
        _config = config;
        _logger = logger;
    }

    [SlashCommand("claim", "Claim a map and a car to validate.")]
    public async Task Claim([Autocomplete(typeof(MapAutocompleteHandler))] string map)
    {
        using var _ = _logger.BeginScope("/claim {User}", Context.User.GlobalName);
        _logger.LogInformation("User {User} executed /claim", Context.User.GlobalName);
        _logger.LogDebug("Parameter value: {Map}", map);

        await DeferAsync(ephemeral: !IsBotChannel());

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        var convertedMap = await _db.ConvertedMaps
            .Include(x => x.Campaign)
            .FirstOrDefaultAsync(x => x.Name == map);

        if (convertedMap is null)
        {
            _logger.LogInformation("Map {Map} does not exist.", map);
            await FollowupAsync("This map does not exist.", ephemeral: true);
            return;
        }

        var user = Context.User;

        if (convertedMap.ClaimedById == user.Id)
        {
            _logger.LogInformation("User {User} already claimed map {Map}.", user.GlobalName, map);
            await FollowupAsync("You already claimed this map.", ephemeral: !IsBotChannel());
            return;
        }

        if (convertedMap.ClaimedById is not null)
        {
            _logger.LogInformation("Map {Map} is already claimed.", map);
            await FollowupAsync("This map is already claimed.", ephemeral: !IsBotChannel());
            return;
        }

        if (convertedMap.Validated)
        {
            _logger.LogInformation("Map {Map} is already validated.", map);
            await FollowupAsync("This map has not been claimed but is already validated.", ephemeral: !IsBotChannel());
            return;
        }

        if (!await _db.Users.AnyAsync(x => x.Id == user.Id))
        {
            _logger.LogDebug("User does not exist in the database, adding...");
            await _db.Users.AddAsync(new UserModel { Id = user.Id });
        }

        if (convertedMap.Impossible)
        {
            _logger.LogInformation("Map {Map} is impossible, but sending the map...", map);
            await FollowupWithFileAsync(
                fileStream: new MemoryStream(convertedMap.Data),
                fileName: convertedMap.GetFileName(),
                text: $"Cannot claim '{convertedMap.Name}' as it's marked impossible, but you can still try to validate it:",
                ephemeral: !IsBotChannel());
            return;
        }

        _logger.LogInformation("Claiming...");

        convertedMap.ClaimedById = user.Id;
        convertedMap.ClaimedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Claimed in {Time}.", stopwatch.Elapsed);

        await _discordReporter.UpdateStatusDescriptionAsync(convertedMap.Campaign);

        _logger.LogInformation("Claimed map {Map}. Sending the map...", map);

        await FollowupWithFileAsync(
            fileStream: new MemoryStream(convertedMap.Data),
            fileName: convertedMap.GetFileName(),
            text: $"Map '{convertedMap.Name}' claimed.",
            ephemeral: !IsBotChannel());

        _logger.LogInformation("Sent.");
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
                .Where(x => !x.Impossible && !x.Validated && x.ClaimedBy == null && EF.Functions.Like(x.Name, $"%{map}%"))
                .Select(x => new { x.Name })
                .Take(25)
                .ToListAsync();

            return AutocompletionResult.FromSuccess(maps.Select(x => new AutocompleteResult(x.Name, x.Name)));
        }
    }
}
