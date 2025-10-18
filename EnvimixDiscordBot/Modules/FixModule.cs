using Discord;
using Discord.Interactions;
using EnvimixDiscordBot.Services;
using ManiaAPI.NadeoAPI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EnvimixDiscordBot.Modules;

public class FixModule : InteractionModuleBase
{
	[Group("fix", "Fix Envimix things.")]
	public class FixGroupModule : InteractionModuleBase<SocketInteractionContext>
	{
		private readonly CampaignMaker _campaignMaker;
		private readonly DiscordReporter _discordReporter;
		private readonly ILogger<FixModule> _logger;

		public FixGroupModule(CampaignMaker campaignMaker, DiscordReporter discordReporter, ILogger<FixModule> logger)
		{
			_campaignMaker = campaignMaker;
			_discordReporter = discordReporter;
			_logger = logger;
		}

		[RequireOwner]
		[SlashCommand("campaign", "Fix unvalidated campaign maps.")]
		public async Task Campaign([Summary("campaign"), Autocomplete(typeof(LocalCampaignAutocompleteHandler))] int campaignId)
		{
			using var _ = _logger.BeginScope("/fix campaign {User}", Context.User.GlobalName);
			_logger.LogInformation("User {User} executed /fix campaign", Context.User.GlobalName);
			_logger.LogDebug("Parameter value: {CampaignId}", campaignId);

			await DeferAsync(ephemeral: true);

			var campaignModel = await _campaignMaker.FixEnvimixCampaignAsync(campaignId);

			if (campaignModel is null)
			{
				_logger.LogWarning("Envimix campaign cannot be fixed.");
				await FollowupAsync("Envimix campaign cannot be fixed.", ephemeral: true);
				return;
			}

            await _discordReporter.UpdateStatusDescriptionAsync(campaignModel);
            await _discordReporter.UpdateNewsMessageAsync(campaignModel);

			_logger.LogInformation("Envimix campaign '{Campaign}' fixed successfully.", campaignModel.Name);

			await FollowupAsync($"Envimix campaign '{campaignModel.Name}' fixed successfully.", ephemeral: true);
        }

        [RequireOwner]
        [SlashCommand("map", "Fix specific map.")]
        public async Task Map([Summary("campaign"), Autocomplete(typeof(LocalCampaignAutocompleteHandler))] int campaignId, [MinValue(1), MaxValue(25)] int mapNum)
        {
            using var _ = _logger.BeginScope("/fix map {User}", Context.User.GlobalName);
            _logger.LogInformation("User {User} executed /fix map", Context.User.GlobalName);
            _logger.LogDebug("Parameter value: {CampaignId} {MapNum}", campaignId, mapNum);

            await DeferAsync(ephemeral: true);

            var campaignModel = await _campaignMaker.FixEnvimixMapAsync(campaignId, mapNum);

            if (campaignModel is null)
            {
                _logger.LogWarning("Envimix map cannot be fixed.");
                await FollowupAsync("Envimix map cannot be fixed.", ephemeral: true);
                return;
            }

            await _discordReporter.UpdateNewsMessageAsync(campaignModel);
            await _discordReporter.UpdateStatusDescriptionAsync(campaignModel);

            _logger.LogInformation("Envimix map in '{Campaign}' fixed successfully.", campaignModel.Name);

            await FollowupAsync($"Envimix campaign '{campaignModel.Name}' fixed successfully.", ephemeral: true);
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
                    .Where(x => EF.Functions.Like(x.Name, $"%{map}%"))
					.Select(x => new { x.OriginalName, x.OriginalUid })
                    .Take(25)
                    .ToListAsync();

                return AutocompletionResult.FromSuccess(maps.DistinctBy(x => x.OriginalUid).Select(x => new AutocompleteResult(x.OriginalName, x.OriginalUid)));
            }
        }
    }
}