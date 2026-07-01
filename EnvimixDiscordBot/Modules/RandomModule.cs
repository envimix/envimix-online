using Discord;
using Discord.Interactions;
using EnvimixDiscordBot.Models;
using EnvimixDiscordBot.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EnvimixDiscordBot.Modules;

[Group("random", "Random map commands.")]
public class RandomModule : InteractionModuleBase
{
    private readonly AppDbContext _db;
    private readonly DiscordReporter _discordReporter;
    private readonly IConfiguration _config;
    private readonly ILogger<RandomModule> _logger;

    public RandomModule(AppDbContext db, DiscordReporter discordReporter, IConfiguration config, ILogger<RandomModule> logger)
    {
        _db = db;
        _discordReporter = discordReporter;
        _config = config;
        _logger = logger;
    }

    [SlashCommand("claim", "Claim a random unclaimed map, optionally filtered by car.")]
    public async Task RandomClaim([Autocomplete(typeof(CarAutocompleteHandler))] string? car = null)
    {
        using var _ = _logger.BeginScope("/random claim {User}", Context.User.GlobalName);
        _logger.LogInformation("User {User} executed /random claim", Context.User.GlobalName);
        _logger.LogDebug("Parameter value: {Car}", car);

        await DeferAsync(ephemeral: !IsBotChannel());

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        var query = _db.ConvertedMaps
            .Include(x => x.Campaign)
            .Where(x => !x.Impossible && !x.Validated && x.ClaimedById == null);

        if (!string.IsNullOrEmpty(car))
        {
            query = query.Where(x => x.CarId == car);
        }

        var convertedMap = await query
            .OrderBy(x => EF.Functions.Random())
            .FirstOrDefaultAsync();

        if (convertedMap is null)
        {
            var noMapsMsg = string.IsNullOrEmpty(car)
                ? "There are no unclaimed maps available."
                : $"There are no unclaimed maps available for car '{car}'.";

            _logger.LogInformation("{Msg}", noMapsMsg);
            await FollowupAsync(noMapsMsg, ephemeral: true);
            return;
        }

        var user = Context.User;

        if (!await _db.Users.AnyAsync(x => x.Id == user.Id))
        {
            _logger.LogDebug("User does not exist in the database, adding...");
            await _db.Users.AddAsync(new UserModel { Id = user.Id });
        }

        _logger.LogInformation("Claiming random map {Map}...", convertedMap.Name);

        convertedMap.ClaimedById = user.Id;
        convertedMap.ClaimedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Claimed in {Time}.", stopwatch.Elapsed);

        await _discordReporter.UpdateStatusDescriptionAsync(convertedMap.Campaign);

        _logger.LogInformation("Claimed map {Map}. Sending the map...", convertedMap.Name);

        await FollowupWithFileAsync(
            fileStream: new MemoryStream(convertedMap.Data),
            fileName: convertedMap.GetFileName(),
            text: $"Random map '{convertedMap.Name}' claimed.",
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

    public class CarAutocompleteHandler : AutocompleteHandler
    {
        public override async Task<AutocompletionResult> GenerateSuggestionsAsync(
            IInteractionContext context,
            IAutocompleteInteraction autocompleteInteraction,
            IParameterInfo parameter,
            IServiceProvider services)
        {
            var input = autocompleteInteraction.Data.Current.Value.ToString() ?? "";

            await using var scope = services.CreateAsyncScope();

            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var cars = await db.ConvertedMaps
                .Where(x => !x.Impossible && !x.Validated && x.ClaimedById == null
                    && EF.Functions.Like(x.CarId, $"%{input}%"))
                .Select(x => x.CarId)
                .Distinct()
                .Take(25)
                .ToListAsync();

            return AutocompletionResult.FromSuccess(cars.Select(x => new AutocompleteResult(x, x)));
        }
    }
}
