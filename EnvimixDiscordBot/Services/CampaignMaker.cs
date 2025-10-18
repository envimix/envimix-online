using EnvimixDiscordBot.Models;
using GBX.NET.Engines.Game;
using GBX.NET;
using ManiaAPI.NadeoAPI;
using Microsoft.EntityFrameworkCore;
using System.Text;
using TmEssentials;
using Microsoft.Extensions.Logging;
using System.Collections.Immutable;

namespace EnvimixDiscordBot.Services;

public sealed class CampaignMaker
{
    private readonly NadeoLiveServices _nls;
    private readonly HttpClient _http;
    private readonly AppDbContext _db;
    private readonly ILogger<CampaignMaker> _logger;

    public CampaignMaker(NadeoLiveServices nls, HttpClient http, AppDbContext db, ILogger<CampaignMaker> logger)
    {
        _nls = nls;
        _http = http;
        _db = db;
        _logger = logger;
    }

    public async Task UpdateTrackingMessageIdsAsync(CampaignModel campaign, Announcement announcement)
    {
        var dbCampaign = await _db.Campaigns
            .FirstOrDefaultAsync(x => x.Id == campaign.Id)
            ?? throw new InvalidOperationException("Campaign not found.");

        dbCampaign.NewsChannelId = announcement.NewsMessage.Channel.Id;
        dbCampaign.NewsMessageId = announcement.NewsMessage.Id;
        dbCampaign.StatusChannelId = announcement.StatusMessage.Channel.Id;
        dbCampaign.StatusMessageId = announcement.StatusMessage.Id;

        await _db.SaveChangesAsync();
    }

    public async Task<CampaignModel?> CreateEnvimixCampaignAsync(ulong? submitterId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Attempting to create a seasonal campaign...");

        var latestCampaignCollection = await _nls.GetSeasonalCampaignsAsync(1, cancellationToken: cancellationToken);
        var latestCampaign = latestCampaignCollection.CampaignList.First();

        return await CreateEnvimixCampaignAsync(latestCampaign, submitterId, cancellationToken);
    }

    public async Task<CampaignModel?> CreateEnvimixCampaignAsync(Campaign campaign, ulong? submitterId, CancellationToken cancellationToken = default)
    {
        var submitter = default(UserModel);

        if (submitterId.HasValue)
        {
            _logger.LogInformation("Handling submitter (Discord ID: {SubmitterId})...", submitterId);

            submitter = await _db.Users.FirstOrDefaultAsync(x => x.Id == submitterId, cancellationToken);

            if (submitter is null)
            {
                submitter = new UserModel
                {
                    Id = submitterId.Value
                };

                await _db.Users.AddAsync(submitter, cancellationToken);
            }
        }

        var campaignModel = await _db.Campaigns.FirstOrDefaultAsync(x => x.Id == campaign.Id, cancellationToken);

        if (campaignModel is not null)
        {
            _logger.LogInformation("Campaign '{CampaignName}' already exists.", campaignModel.Name);
            return null;
        }

        _logger.LogInformation("Creating '{CampaignName}' campaign... (club ID: {ClubId}, campaign ID: {CampaignId})", campaign.Name, campaign.ClubId, campaign.Id);

        campaignModel = new CampaignModel
        {
            Name = campaign.Name,
            Id = campaign.Id,
            Submitter = submitter,
            ClubId = campaign.ClubId
        };

        await _db.Campaigns.AddAsync(campaignModel, cancellationToken);

        _logger.LogInformation("Requesting {MapCount} map infos...", campaign.Playlist.Length);

        var mapInfos = await _nls.GetMapInfosAsync(campaign.Playlist.Select(x => x.MapUid), cancellationToken: cancellationToken);

        for (int i = 0; i < mapInfos.Length; i++)
        {
            await AddConvertedMapsAsync(campaignModel, mapInfos, i, orderOffset: 0, cancellationToken);
        }

        _logger.LogInformation("Saving changes to the database...");

        await _db.SaveChangesAsync(cancellationToken);

        return campaignModel;
	}

    private async Task AddConvertedMapsAsync(CampaignModel campaignModel, ImmutableArray<MapInfoLive> mapInfos, int i, int orderOffset, CancellationToken cancellationToken)
    {
        var mapInfo = mapInfos[i];

        var deformattedOriginalMapName = TextFormatter.Deformat(mapInfo.Name);

        _logger.LogInformation("Processing map {MapIndex}/{MapCount} ({MapName})...", i + 1, mapInfos.Length, deformattedOriginalMapName);
        using var downloadResponse = await _http.GetAsync(mapInfo.DownloadUrl, cancellationToken);
        using var stream = await downloadResponse.Content.ReadAsStreamAsync(cancellationToken);
        _logger.LogInformation("Generating envimixes for map {MapIndex}/{MapCount} ({MapName})...", i + 1, mapInfos.Length, deformattedOriginalMapName);

        var carDict = new Dictionary<string, CarModel>();

        await foreach (var generatedMap in GenerateEnvimixMapsAsync(stream))
        {
            _logger.LogDebug("Serializing converted map '{MapName}'...", generatedMap.MapName);

            using var ms = new MemoryStream();
            generatedMap.Save(ms);

            var playerModelId = generatedMap.PlayerModel!.Id;

            var car = await _db.Cars.FindAsync([playerModelId], cancellationToken);

            if (car is null)
            {
                car = new CarModel
                {
                    Id = playerModelId
                };

                _logger.LogInformation("Adding car '{CarId}' to the database...", playerModelId);

                await _db.Cars.AddAsync(car, cancellationToken);
            }

            _logger.LogDebug("Adding converted map '{MapName}' to the database...", generatedMap.MapName);

            var mapModel = new ConvertedMapModel
            {
                Uid = generatedMap.MapUid,
                Name = TextFormatter.Deformat(generatedMap.MapName),
                CarId = playerModelId,
                Car = car,
                Campaign = campaignModel,
                OriginalUid = mapInfo.Uid,
                OriginalName = deformattedOriginalMapName,
                Data = ms.ToArray(),
                Order = i + 1 + orderOffset,
                LastModifiedAt = mapInfo.UpdateTimestamp,
            };

            await _db.ConvertedMaps.AddAsync(mapModel, cancellationToken);
        }
    }

    public async Task<CampaignModel?> FixEnvimixCampaignAsync(int campaignId, CancellationToken cancellationToken = default)
	{
        var campaign = await _db.Campaigns
			.FirstOrDefaultAsync(x => x.Id == campaignId, cancellationToken);

        if (campaign is null)
        {
			_logger.LogWarning("Campaign {CampaignId} not found.", campaignId);
			return null;
		}

        _logger.LogInformation("Fixing campaign '{CampaignName}'...", campaign.Name);

        var maps = await _db.ConvertedMaps
            .Where(x => x.CampaignId == campaignId)
            .ToListAsync(cancellationToken);

        var mapsByOriginalUidAndCarId = maps
            .DistinctBy(x => x.OriginalUid)
            .ToDictionary(x => (x.OriginalUid, x.CarId));

        if (maps.Count == 0)
        {
			_logger.LogWarning("No maps to fix in campaign '{CampaignName}'.", campaign.Name);
			return null;
		}

        _logger.LogInformation("Checking {MapCount} maps in campaign '{CampaignName}'...", maps.Count, campaign.Name);

        foreach (var map in maps.DistinctBy(x => x.OriginalUid))
		{
			_logger.LogInformation("Checking map '{OriginalName}'...", map.OriginalName);

			var mapInfo = await _nls.GetMapInfoAsync(map.OriginalUid, cancellationToken);

            if (map.Validated && (map.LastModifiedAt is null || map.LastModifiedAt >= mapInfo.UpdateTimestamp))
            {
                map.LastModifiedAt ??= mapInfo.UpdateTimestamp;
                _logger.LogInformation("Map '{MapName}' is up to date (LastModifiedAt).", map.Name);
                continue;
            }

            using var downloadResponse = await _http.GetAsync(mapInfo.DownloadUrl, cancellationToken);
            await using var stream = await downloadResponse.Content.ReadAsStreamAsync(cancellationToken);

			_logger.LogInformation("Generating envimixes for map '{MapName}'...", map.Name);

			await foreach (var generatedMap in GenerateEnvimixMapsAsync(stream))
			{
				var playerModelId = generatedMap.PlayerModel!.Id;

				var mapModel = mapsByOriginalUidAndCarId.GetValueOrDefault((map.OriginalUid, playerModelId));

				if (mapModel is null)
				{
					continue;
				}

				_logger.LogDebug("Serializing converted map '{MapName}'...", generatedMap.MapName);

				using var ms = new MemoryStream();
				generatedMap.Save(ms);

				_logger.LogDebug("Updating converted map '{MapName}' in the database...", generatedMap.MapName);

                mapModel.Data = ms.ToArray();
                mapModel.Name = TextFormatter.Deformat(generatedMap.MapName);
                mapModel.Uid = generatedMap.MapUid;
                mapModel.LastModifiedAt = mapInfo.UpdateTimestamp;
                mapModel.Validated = false;
                mapModel.ClaimedBy = null; // PROBLEM this causes claim resets in cases where its not needed
                mapModel.ClaimedById = null; // PROBLEM this causes claim resets in cases where its not needed
                mapModel.ClaimedAt = null; // PROBLEM this causes claim resets in cases where its not needed
            }
        }

        // check for new maps in club campaigns
        if (campaign.ClubId.HasValue && campaign.ClubId != 0)
        {
            var clubCampaign = await _nls.GetClubCampaignAsync(campaign.ClubId.GetValueOrDefault(), campaignId, cancellationToken);

            var newMaps = clubCampaign.Campaign
                .Playlist
                .Select(x => x.MapUid)
                .Where(x => !maps.Any(y => y.OriginalUid == x));

            var mapInfos = await _nls.GetMapInfosAsync(newMaps, cancellationToken: cancellationToken);
        
            for (int i = 0; i < mapInfos.Length; i++)
            {
                await AddConvertedMapsAsync(campaign, mapInfos, i, orderOffset: maps.Count, cancellationToken);
            }

            // remove unvalidated duplicates
            var duplicates = maps
                .OrderByDescending(x => x.Validated)
                .GroupBy(x => (x.OriginalUid, x.CarId))
                .Where(x => x.Count() > 1)
                .SelectMany(x => x.Skip(1));

            _db.ConvertedMaps.RemoveRange(duplicates);
        }

        _logger.LogInformation("Saving changes to the database...");

        await _db.SaveChangesAsync(cancellationToken);

        return campaign;
    }

    internal async Task<CampaignModel?> FixEnvimixMapAsync(int campaignId, int mapNum, CancellationToken cancellationToken = default)
    {
        var campaign = await _db.Campaigns
            .FirstOrDefaultAsync(x => x.Id == campaignId, cancellationToken);

        if (campaign is null)
        {
            _logger.LogWarning("Campaign not found.");
            return null;
        }

        var latestCampaignCollection = await _nls.GetSeasonalCampaignsAsync(1, cancellationToken: cancellationToken);
        var latestCampaign = latestCampaignCollection.CampaignList.First();

        if (campaignId != latestCampaign.Id)
        {
            _logger.LogWarning("Campaign {CampaignId} is not the latest campaign.", campaignId);
            return null;
        }

        var maps = await _db.ConvertedMaps
            .Where(x => x.CampaignId == campaignId && x.Order == mapNum)
            .ToListAsync(cancellationToken);

        if (maps.Count == 0)
        {
            _logger.LogWarning("Converted maps not found.");
            return null;
        }

        var mapInfo = await _nls.GetMapInfoAsync(latestCampaign.Playlist[mapNum - 1].MapUid, cancellationToken);

        _logger.LogInformation("Fixing map {Map}...", mapInfo.Name);

        using var downloadResponse = await _http.GetAsync(mapInfo.DownloadUrl, cancellationToken);
        await using var stream = await downloadResponse.Content.ReadAsStreamAsync(cancellationToken);

        _logger.LogInformation("Generating envimixes for map {Map}...", mapInfo.Name);

        await foreach (var generatedMap in GenerateEnvimixMapsAsync(stream))
        {
            _logger.LogDebug("Serializing converted map '{MapName}'...", generatedMap.MapName);
            using var ms = new MemoryStream();
            generatedMap.Save(ms);

            _logger.LogDebug("Updating converted map '{MapName}' in the database...", generatedMap.MapName);

            var playerModelId = generatedMap.PlayerModel!.Id;

            var mapModel = maps.FirstOrDefault(x => x.CarId == playerModelId);

            if (mapModel is null)
            {
                continue;
            }

            mapModel.Data = ms.ToArray();
            mapModel.Name = TextFormatter.Deformat(generatedMap.MapName);
            mapModel.Uid = generatedMap.MapUid;
            mapModel.OriginalUid = mapInfo.Uid;
            mapModel.OriginalName = TextFormatter.Deformat(mapInfo.Name);
            mapModel.LastModifiedAt = mapInfo.UpdateTimestamp;
            mapModel.Validated = false;
            mapModel.ClaimedBy = null;
            mapModel.ClaimedById = null;
            mapModel.ClaimedAt = null;
        }

        _logger.LogInformation("Saving changes to the database...");

        await _db.SaveChangesAsync(cancellationToken);

        return campaign;
    }

    private readonly string[] cars = ["CarSport", "CarSnow", "CarRally", "CarDesert"];
    private readonly string[] envs = ["Stadium", "Snow", "Rally", "Desert"];
    private readonly string mapFormat = "$<{0}$> - {1}";

    private async IAsyncEnumerable<CGameCtnChallenge> GenerateEnvimixMapsAsync(Stream stream)
    {
        var map = await Gbx.ParseNodeAsync<CGameCtnChallenge>(stream);

        var defaultCar = map.PlayerModel?.Id;
        if (string.IsNullOrEmpty(defaultCar))
        {
            defaultCar = "CarSport";
        }

        var defaultMapUid = map.MapUid;
        var defaultMapName = map.MapName;

        for (int i = 0; i < cars.Length; i++)
        {
            if (i > 0)
            {
                stream.Seek(0, SeekOrigin.Begin);
                map = await Gbx.ParseNodeAsync<CGameCtnChallenge>(stream);
            }

            var car = cars[i];
            var env = envs.Length > i ? envs[i] : null;

            var include = true;
            if (!include)
            {
                continue;
            }

            var originalMapUid = map.MapUid;
            var originalAuthorLogin = map.AuthorLogin;
            var originalAuthorNickname = map.AuthorNickname;

            map.PlayerModel = new Ident(car, 10003, "Nadeo");
            map.MapUid = $"{Convert.ToBase64String(Encoding.ASCII.GetBytes(Guid.NewGuid().ToString()))[..10]}{defaultMapUid.Substring(9, 10)}ENVIMIX";
            map.MapName = string.Format(mapFormat, defaultMapName, car);

            if (env is not null)
            {
                var someGatesChanged = ChangeGates(map, env);

                if (!someGatesChanged && car == defaultCar)
                {
                    continue;
                }
            }

            var invalidTime = map.MapType == "TrackMania\\TM_Stunt"
                ? TimeInt32.Zero
				: TimeInt32.MaxValue;

			map.AuthorTime = invalidTime;
            map.GoldTime = invalidTime;
            map.SilverTime = invalidTime;
            map.BronzeTime = invalidTime;

            if (map.ScriptMetadata is not null)
            {
                map.ScriptMetadata.Declare("ENVIMIX_IsConverted", true);
                map.ScriptMetadata.Declare("ENVIMIX_Car", car);
                map.ScriptMetadata.Declare("ENVIMIX_OriginalMapUid", originalMapUid);
                map.ScriptMetadata.Declare("ENVIMIX_OriginalAuthorLogin", originalAuthorLogin);

                if (originalAuthorNickname is not null)
                {
                    map.ScriptMetadata.Declare("ENVIMIX_OriginalAuthorNickname", originalAuthorNickname);
                }
            }

            map.Chunks.Remove<CGameCtnChallenge.Chunk0304305B>();
            map.RemovePassword();

            yield return map;
        }
    }

    private bool ChangeGates(CGameCtnChallenge map, string envimixEnvironment)
    {
        var someGatesChanged = false;
        var gatesToRemove = new List<CGameCtnAnchoredObject>();

        foreach (var block in map.GetBlocks().Where(block => block.Name.Contains("Gameplay")))
        {
            for (int i = 0; i < envs.Length; i++)
            {
                var env = envs[i];

                if (block.Name.Contains($"Gameplay{env}"))
                {
                    block.Name = block.Name.Replace(env, envimixEnvironment);
                    someGatesChanged = true;
                }
            }
        }

        foreach (var item in map.GetAnchoredObjects().Where(item => item.ItemModel.Id.Contains("Gameplay")))
        {
            for (int i = 0; i < envs.Length; i++)
            {
                var env = envs[i];

                if (item.ItemModel.Id.Contains($"Gameplay{env}"))
                {
                    item.ItemModel = item.ItemModel with { Id = item.ItemModel.Id.Replace(env, envimixEnvironment) };
                    someGatesChanged = true;
                }

                if (item.ItemModel.Id.Contains($"{env}GateGameplay"))
                {
                    if (envimixEnvironment == "Stadium")
                    {
                        gatesToRemove.Add(item);
                    }
                    else
                    {
                        item.ItemModel = item.ItemModel with { Id = item.ItemModel.Id.Replace(env, envimixEnvironment) };
                        someGatesChanged = true;
                    }
                }
            }
        }

        foreach (var gate in gatesToRemove)
        {
            map.AnchoredObjects!.Remove(gate);
            someGatesChanged = true;
        }

        return someGatesChanged;
    }
}
