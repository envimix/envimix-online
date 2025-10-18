using Discord.Interactions;
using Discord;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Text;

namespace EnvimixDiscordBot.Modules;

public class ListModule : InteractionModuleBase
{
    [Group("list", "Get a list of something.")]
    public class ListGroupModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;
        private readonly ILogger<ListModule> _logger;

        public ListGroupModule(AppDbContext db, IConfiguration config, ILogger<ListModule> logger)
        {
            _db = db;
            _config = config;
            _logger = logger;
        }

        [SlashCommand("claims", "List all unfinished claims.")]
        public async Task Claims([Summary("campaign"), Autocomplete(typeof(LocalCampaignAutocompleteHandler))] int campaignId)
        {
            using var _ = _logger.BeginScope("/list claims {User}", Context.User.GlobalName);
            _logger.LogInformation("User {User} executed /list claims", Context.User.GlobalName);
            _logger.LogDebug("Parameter value: {CampaignId}", campaignId);

            var campaignModel = await _db.Campaigns
                .FirstOrDefaultAsync(x => x.Id == campaignId);

            if (campaignModel is null)
            {
                _logger.LogWarning("Envimix campaign not found.");
                await RespondAsync("Envimix campaign not found.", ephemeral: true);
                return;
            }

            var maps = await _db.ConvertedMaps
                .Where(x => x.CampaignId == campaignId && x.ClaimedById.HasValue && !x.Validated && !x.Impossible)
                .Select(x => new
                {
                    x.ClaimedById,
                    x.OriginalUid,
                    x.OriginalName,
                    x.CarId
                })
                .ToListAsync();

            var sb = new StringBuilder();

            if (maps.Count == 0)
            {
                sb.AppendLine("None.");
            }

            var counter = 1;
            
            foreach (var userClaim in maps
                .GroupBy(x => x.ClaimedById.GetValueOrDefault())
                .OrderByDescending(x => x.Count()))
            {
                sb.AppendLine($"{counter}. **{MentionUtils.MentionUser(userClaim.Key)}**: {userClaim.Count()}");

                foreach (var map in userClaim.GroupBy(x => x.OriginalUid))
                {
                    sb.Append($"  - {map.First().OriginalName} - ");
                    sb.AppendJoin(", ", map.Select(x => x.CarId));
                    sb.AppendLine();
                }

                counter++;
            }

            _logger.LogInformation("Listed claims on {CampaignName} successfully.", campaignModel.Name);

            await RespondAsync(embed: new EmbedBuilder()
                .WithTitle($"Claims on {campaignModel.Name}")
                .WithDescription(sb.ToString())
                .Build(), ephemeral: !IsBotChannel());
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

        public class LocalCampaignAutocompleteHandler : AutocompleteHandler
        {
            public override async Task<AutocompletionResult> GenerateSuggestionsAsync(
                IInteractionContext context,
                IAutocompleteInteraction autocompleteInteraction,
                IParameterInfo parameter,
                IServiceProvider services)
            {
                var campaignName = autocompleteInteraction.Data.Current.Value.ToString() ?? "";

                await using var scope = services.CreateAsyncScope();

                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var campaigns = await db.Campaigns
                    .Where(x => EF.Functions.Like(x.Name, $"%{campaignName}%"))
                    .Take(25)
                    .ToListAsync();

                return AutocompletionResult.FromSuccess(campaigns.Select(x => new AutocompleteResult(x.Name, x.Id)));
            }
        }
    }
}