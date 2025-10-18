using Discord;
using EnvimixDiscordBot.Extensions;
using EnvimixDiscordBot.Models;
using ManiaAPI.NadeoAPI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.IO.Compression;
using System.Text;

namespace EnvimixDiscordBot.Services;

public sealed class DiscordReporter
{
    private readonly AppDbContext _db;
    private readonly IDiscordBot _bot;
    private readonly IConfiguration _config;
    private readonly ILogger<DiscordReporter> _logger;

    public DiscordReporter(
        AppDbContext db,
        IDiscordBot bot,
        IConfiguration config,
        ILogger<DiscordReporter> logger)
    {
        _db = db;
        _bot = bot;
        _config = config;
        _logger = logger;
    }

    public async Task<Announcement> AnnounceCampaignAsync(CampaignModel campaign, CancellationToken cancellationToken = default)
    {
        var statusMessage = await SendCampaignStatusAsync(campaign, cancellationToken);
        var newsMessage = await SendNewsMessage(campaign, cancellationToken);

        return new Announcement(newsMessage!, statusMessage!);
    }

    public async Task<IUserMessage?> SendCampaignStatusAsync(CampaignModel campaign, CancellationToken cancellationToken = default)
    {
        var statusChannelId = ulong.Parse(_config.GetRequiredValue("TM2020:StatusChannelId"));

        _logger.LogInformation("Sending campaign status to Discord...");

        return await _bot.SendMessageAsync(statusChannelId, embed: new EmbedBuilder()
            .WithTitle(campaign.Name)
            .WithDescription(GetStatusDescription(campaign))
            .WithCurrentTimestamp()
            .Build(), cancellationToken: default);
    }

    private async Task<IUserMessage?> SendNewsMessage(CampaignModel campaign, CancellationToken cancellationToken)
    {
        var newsChannelId = ulong.Parse(_config.GetRequiredValue("TM2020:NewsChannelId"));

        var attachments = await CreateNewsZipFilesAsync(campaign, cancellationToken);

        _logger.LogInformation("Sending campaign announcement to Discord...");

        return await _bot.SendMessageAsync(newsChannelId,
            GetNewsCampaignMessage(campaign),
            attachments: attachments, cancellationToken: default);
    }

    public async Task CheckCampaignCompletionAsync(CampaignModel campaign, string car, CancellationToken cancellationToken = default)
    {
        if (campaign.StatusChannelId is null || campaign.StatusMessageId is null)
        {
            return;
        }

        var anyMapNotCompleteInCarCampaign = await _db.ConvertedMaps
            .AnyAsync(x =>
                x.CampaignId == campaign.Id
             && x.CarId == car
             && !x.Validated
             && !x.Impossible, cancellationToken);

        if (anyMapNotCompleteInCarCampaign)
        {
            return;
        }

        var campaignCarMaps = await _db.ConvertedMaps
            .Include(x => x.Campaign)
            .Where(x => x.CampaignId == campaign.Id && x.CarId == car)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var allCampaignMapsComplete = await _db.ConvertedMaps
            .Where(x => x.CampaignId == campaign.Id)
            .AllAsync(x => x.Validated || x.Impossible, cancellationToken);

        _logger.LogInformation("Campaign {CampaignName} is completed with {Car}.", campaign.Name, car);

        await _bot.ModifyMessageAsync(campaign.StatusChannelId.Value, campaign.StatusMessageId.Value, properties =>
        {
            var embedBuilder = new EmbedBuilder()
                .WithTitle(campaign.Name)
                .WithDescription(GetStatusDescription(campaign))
                .WithCurrentTimestamp();

            if (allCampaignMapsComplete)
            {
                embedBuilder = embedBuilder.WithFooter("Completed!");
            }

            properties.Embed = embedBuilder.Build();
        }, cancellationToken);

        var dumpChannelId = ulong.Parse(_config.GetRequiredValue("TM2020:DumpChannelId"));

        var zipAttachments = await DumpCampaignAsync(campaignCarMaps.First().Campaign, cancellationToken);

        if (!zipAttachments.Any())
        {
            return;
        }

        await _bot.SendMessageAsync(dumpChannelId,
            $"## {campaign.Name} Envimix Campaign completed with {car}!",
            attachments: zipAttachments.Take(1), cancellationToken: cancellationToken);

        foreach (var attachment in zipAttachments.Skip(1))
        {
            await _bot.SendMessageAsync(dumpChannelId, attachments: [attachment], cancellationToken: cancellationToken);
        }
    }

    public async Task<IEnumerable<FileAttachment>> DumpCampaignAsync(int campaignId, string? specificCar = null, CancellationToken cancellationToken = default)
    {
        var allCampaignMaps = await _db.ConvertedMaps
            .Include(x => x.Campaign)
            .Where(x => x.CampaignId == campaignId)
            .ToListAsync(cancellationToken);

        return await DumpCampaignAsync(allCampaignMaps.First().Campaign, cancellationToken);
    }

    private async Task<IEnumerable<FileAttachment>> DumpCampaignAsync(CampaignModel campaign, CancellationToken cancellationToken = default)
    {
        var zipAttachments = new List<FileAttachment>();

        foreach (var mapGrouping in campaign.Maps.GroupBy(x => x.CarId))
        {
            var ms = new MemoryStream();
            var zip = new ZipArchive(ms, ZipArchiveMode.Create, true);
            var totalSize = 0;
            var carCounter = 1;

            foreach (var map in mapGrouping)
            {
                totalSize += map.Data.Length;

                if (totalSize > 10_000_000)
                {
                    zip.Dispose();
                    zipAttachments.Add(new FileAttachment(ms, $"{campaign.Name} - {mapGrouping.Key} ({carCounter}).zip"));
                    _logger.LogInformation("ZIP for car {CarId} size: {Size}", mapGrouping.Key, totalSize);

                    carCounter++;
                    ms = new MemoryStream();
                    zip = new ZipArchive(ms, ZipArchiveMode.Create, true);
                    totalSize = map.Data.Length;
                }

                _logger.LogInformation("Adding map {MapName} to ZIP... (total size: {Size})", map.OriginalName, totalSize);

                var entry = zip.CreateEntry($"{campaign.Name} - {mapGrouping.Key}/{map.OriginalName} - {map.CarId}.Map.Gbx");
                await using var entryStream = entry.Open();
                await entryStream.WriteAsync(map.Data, cancellationToken);
            }

            zip.Dispose();
            zipAttachments.Add(new FileAttachment(ms, $"{campaign.Name} - {mapGrouping.Key} ({carCounter}).zip"));

            _logger.LogInformation("ZIP for car {CarId} size: {Size}", mapGrouping.Key, totalSize);
        }

        foreach (var attachment in zipAttachments)
        {
            _logger.LogInformation("ZIP attachment: {Name} ({Size})", attachment.FileName, attachment.Stream.Length);
        }

        return zipAttachments;
    }

    public async Task UpdateStatusDescriptionAsync(CampaignModel campaign)
    {
        campaign = await _db.Campaigns
            .Include(x => x.Maps)
            .FirstAsync(x => x.Id == campaign.Id);

        _logger.LogInformation("Updating campaign status description...");

        if (campaign.StatusChannelId.HasValue && campaign.StatusMessageId.HasValue)
        {
            await _bot.ModifyMessageAsync(campaign.StatusChannelId.Value, campaign.StatusMessageId.Value, properties =>
            {
                properties.Embed = new EmbedBuilder()
                    .WithTitle(campaign.Name)
                    .WithDescription(GetStatusDescription(campaign))
                    .WithCurrentTimestamp()
                    .Build();
            });
        }
        else
        {
            var statusMessage = await SendCampaignStatusAsync(campaign);
            campaign.StatusChannelId = statusMessage!.Channel.Id;
            campaign.StatusMessageId = statusMessage!.Id;
            await _db.SaveChangesAsync();
        }
    }

    public async Task UpdateNewsMessageAsync(CampaignModel campaign, CancellationToken cancellationToken = default)
    {
        campaign = await _db.Campaigns.FirstAsync(x => x.Id == campaign.Id, cancellationToken);

        _logger.LogInformation("Updating campaign news description...");

        var zips = (await CreateNewsZipFilesAsync(campaign, cancellationToken)).ToList();

        if (campaign.NewsChannelId.HasValue && campaign.NewsMessageId.HasValue)
        {
            await _bot.ModifyMessageAsync(campaign.NewsChannelId.Value, campaign.NewsMessageId.Value, properties =>
            {
                properties.Content = GetNewsCampaignMessage(campaign);
                properties.Attachments = new(zips);
            }, cancellationToken);
        }
        else
        {
            var newsMessage = await SendNewsMessage(campaign, cancellationToken);
            campaign.NewsChannelId = newsMessage!.Channel.Id;
            campaign.NewsMessageId = newsMessage.Id;
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

	private static string GetNewsCampaignMessage(CampaignModel campaign)
	{
		return $"## {campaign.Name} Envimix Campaign!\n\nNew envimix campaign is here! <@&1324068076009951294>\n1. Claim map combinations with `/claim`\n2. Download the ZIP with all the maps.\n3. Send validations with `/validate`.\n**Drive reasonable author times.** Make sure to have Editor++ plugin installed for the vehicle fix.";
	}

	private async Task<IEnumerable<FileAttachment>> CreateNewsZipFilesAsync(CampaignModel campaign, CancellationToken cancellationToken)
	{
		var zipDict = new Dictionary<string, (MemoryStream, ZipArchive)>();

		_logger.LogInformation("Creating envimix ZIPs for campaign {CampaignName}...", campaign.Name);

		foreach (var mapGrouping in campaign.Maps.Where(x => !x.Validated).GroupBy(x => x.OriginalUid))
		{
			foreach (var convertedMap in mapGrouping)
			{
				if (!zipDict.TryGetValue(convertedMap.CarId, out var streamAndZip))
				{
					_logger.LogInformation("Creating ZIP for car {CarId}...", convertedMap.CarId);
					var stream = new MemoryStream();
					var zip = new ZipArchive(stream, ZipArchiveMode.Create, true)
					{
						Comment = $"Automatically generated ZIP containing {convertedMap.Car} envimix version of {campaign.Name} campaign."
					};
					streamAndZip = (stream, zip);

					zipDict[convertedMap.CarId] = streamAndZip;
				}

				var entry = streamAndZip.Item2.CreateEntry($"{campaign.Name}/{convertedMap.OriginalName} - {convertedMap.CarId}.Map.Gbx");

				using var entryStream = entry.Open();
				await entryStream.WriteAsync(convertedMap.Data, cancellationToken);
			}
		}

        // when nadeo includes custom items, this can exceed, otherwise its ok
        if (zipDict.Sum(x => x.Value.Item1.Length) > 25_000_000)
        {
            return [];
        }

		foreach (var (car, (stream, zip)) in zipDict)
		{
			zip.Dispose();
			_logger.LogInformation("ZIP for car {CarId} size: {Size}", car, stream.Length);
		}

		return zipDict.Select(pair => new FileAttachment(pair.Value.Item1, $"{campaign.Name} - {pair.Key}.zip"));
	}

	private string GetStatusDescription(CampaignModel campaign)
    {
        var longestMapName = campaign.Maps.Max(x => x.OriginalName.Length);

        var sb = new StringBuilder();

        sb.Append("**`Map");
        sb.Append(' ', longestMapName - 3);
        sb.Append("`** ― ");
        sb.Append(_config.GetRequiredValue("TM2020:Emotes:CarSport"));
        sb.Append(" ― ");
        sb.Append(_config.GetRequiredValue("TM2020:Emotes:CarSnow"));
        sb.Append(" ― ");
        sb.Append(_config.GetRequiredValue("TM2020:Emotes:CarRally"));
        sb.Append(" ― ");
        sb.AppendLine(_config.GetRequiredValue("TM2020:Emotes:CarDesert"));

        foreach (var mapGrouping in campaign.Maps.OrderBy(x => x.Order).GroupBy(x => x.OriginalUid))
        {
            var mapName = mapGrouping.First().OriginalName;

            sb.Append('`');
            sb.Append(mapName);
            sb.Append(' ', longestMapName - mapName.Length);
            sb.Append('`');

            foreach (var car in new[] { "CarSport", "CarSnow", "CarRally", "CarDesert" })
            {
                sb.Append(" ― ");

                var carSportMap = mapGrouping.FirstOrDefault(x => x.CarId == car);

                if (carSportMap is null)
                {
                    sb.Append("✖️");
                }
                else if (carSportMap.Impossible)
                {
                    sb.Append('❌');
                }
                else if (carSportMap.ClaimedById is null)
                {
                    sb.Append("🟦");
                }
                else if (carSportMap.Validated)
                {
                    sb.Append('✅');
                }
                else
                {
                    sb.Append("🟧");
                }
            }

            sb.AppendLine();
        }

        sb.AppendLine();
        sb.Append("Validators: ");

        var first = true;

        foreach (var userGrouping in campaign.Maps
            .Where(x => x.Validated && x.ClaimedById.HasValue)
            .GroupBy(x => x.ClaimedById)
            .OrderByDescending(x => x.Count()))
        {
            var count = userGrouping.Count();

            if (count == 0)
            {
                continue;
            }

            if (!first)
            {
                sb.Append(", ");
            }

            sb.Append($"<@{userGrouping.Key}> ({count})");

            first = false;
        }

        if (first)
        {
            sb.Append("none yet!");
        }

        return sb.ToString();
    }
}
