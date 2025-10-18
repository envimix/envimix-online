using Discord;
using Discord.Interactions;
using EnvimixDiscordBot.Services;
using ManiaAPI.NadeoAPI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TmEssentials;

namespace EnvimixDiscordBot.Modules;

public class EnvimixModule : InteractionModuleBase
{
    [Group("envimix", "General Envimix commands.")]
    public class EnvimixGroupModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly CampaignMaker _campaignMaker;
        private readonly DiscordReporter _discordReporter;
        private readonly NadeoLiveServices _nls;
        private readonly ILogger<EnvimixModule> _logger;

        public EnvimixGroupModule(CampaignMaker campaignMaker, DiscordReporter discordReporter, NadeoLiveServices nls, ILogger<EnvimixModule> logger)
        {
            _campaignMaker = campaignMaker;
            _discordReporter = discordReporter;
            _nls = nls;
            _logger = logger;
        }

        [RequireOwner]
        [SlashCommand("new", "Create a new envimix campaign manually.")]
        public async Task New([Summary("campaign"), Autocomplete(typeof(CampaignAutocompleteHandler))] string campaignId)
        {
            using var _ = _logger.BeginScope("/envimix new {User}", Context.User.GlobalName);
            _logger.LogInformation("User {User} executed /envimix new", Context.User.GlobalName);
            _logger.LogDebug("Parameter value: {CampaignId}", campaignId);

            await DeferAsync(ephemeral: true);

            var campaignIdSplit = campaignId.Split('-');

            if (campaignIdSplit.Length != 2 || !int.TryParse(campaignIdSplit[0], out var clubId) || !int.TryParse(campaignIdSplit[1], out var campId))
            {
                _logger.LogDebug("Invalid campaign ID.");
                await FollowupAsync("Invalid campaign ID.", ephemeral: true);
                return;
            }

            var campaign = await _nls.GetClubCampaignAsync(clubId, campId);

            _logger.LogDebug("Campaign: {Campaign}", campaign);

            var campaignModel = await _campaignMaker.CreateEnvimixCampaignAsync(campaign.Campaign, Context.User.Id);

            if (campaignModel is null)
            {
                _logger.LogWarning("Envimix campaign already exists.");
                await FollowupAsync("Envimix campaign already exists.", ephemeral: true);
                return;
            }

            var announcement = await _discordReporter.AnnounceCampaignAsync(campaignModel);
            await _campaignMaker.UpdateTrackingMessageIdsAsync(campaignModel, announcement);

            _logger.LogInformation("Manual envimix campaign created and announced.");

            await FollowupAsync($"Envimix campaign '{campaignModel.Name}' created successfully.", ephemeral: true);
        }

        [RequireOwner]
        [SlashCommand("dump", "Dump all envimix campaign maps manually.")]
        public async Task Dump([Summary("campaign"), Autocomplete(typeof(LocalCampaignAutocompleteHandler))] string campaignId)
        {
            using var _ = _logger.BeginScope("/envimix dump {User}", Context.User.GlobalName);
            _logger.LogInformation("User {User} executed /envimix dump", Context.User.GlobalName);
            _logger.LogDebug("Parameter value: {CampaignId}", campaignId);

            if (!int.TryParse(campaignId, out var campId))
            {
                _logger.LogDebug("Invalid campaign ID.");
                await FollowupAsync("Invalid campaign ID.", ephemeral: true);
                return;
            }

            await DeferAsync(ephemeral: false);

            var attachments = await _discordReporter.DumpCampaignAsync(campId);

            if (!attachments.Any())
            {
                _logger.LogWarning("Envimix campaign not found.");
                await FollowupAsync("Envimix campaign not found.", ephemeral: false);
                return;
            }

            await FollowupWithFileAsync(attachments.First(), $"Envimix campaign dumped early.", ephemeral: false);

            foreach (var attachment in attachments.Skip(1))
            {
                await FollowupWithFileAsync(attachment, ephemeral: false);
            }
        }

        public class CampaignAutocompleteHandler : AutocompleteHandler
        {
            public override async Task<AutocompletionResult> GenerateSuggestionsAsync(
                IInteractionContext context,
                IAutocompleteInteraction autocompleteInteraction,
                IParameterInfo parameter,
                IServiceProvider services)
            {
                var campaignName = autocompleteInteraction.Data.Current.Value.ToString() ?? "";

                var nls = services.GetRequiredService<NadeoLiveServices>();

                var campaigns = await nls.GetClubCampaignsAsync(length: 25, offset: 0, campaignName);

                return AutocompletionResult.FromSuccess(campaigns.ClubCampaignList.Select(x => new AutocompleteResult($"{TextFormatter.Deformat(x.Name)} ({x.CampaignId})", $"{x.ClubId}-{x.CampaignId}")));
            }
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

                return AutocompletionResult.FromSuccess(campaigns.Select(x => new AutocompleteResult(x.Name, x.Id.ToString())));
            }
        }
    }
}