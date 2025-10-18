using Discord;
using Discord.Interactions;
using EnvimixDiscordBot.Models;
using EnvimixDiscordBot.Services;
using GBX.NET;
using GBX.NET.Engines.Game;
using GBX.NET.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System.IO.Compression;
using System.Text;
using TmEssentials;

namespace EnvimixDiscordBot.Modules;

public class ValidateModule : InteractionModuleBase
{
    private readonly DiscordReporter _discordReporter;
    private readonly AppDbContext _db;
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly ILogger<ValidateModule> _logger;

    public ValidateModule(
        DiscordReporter discordReporter,
        AppDbContext db,
        HttpClient http,
        IConfiguration config,
        ILogger<ValidateModule> logger)
    {
        _discordReporter = discordReporter;
        _db = db;
        _http = http;
        _config = config;
        _logger = logger;
    }

    [SlashCommand("validate", "Submit a validated map. Also works for ZIP files.")]
    public async Task Validate(IAttachment mapfile)
    {
        using var _ = _logger.BeginScope("/validate {User}", Context.User.GlobalName);
        _logger.LogInformation("User {User} executed /validate", Context.User.GlobalName);

        await DeferAsync(ephemeral: !IsBotChannel());

        var user = Context.User;

        if (mapfile.Size > 10 * 1024 * 1024)
        {
            _logger.LogDebug("File too large.");
            await FollowupAsync("File too large.", ephemeral: !IsBotChannel());
            return;
        }

        using var response = await _http.GetAsync(mapfile.Url);
        await using var stream = await response.Content.ReadAsStreamAsync();
        await using var bufferedStream = new BufferedStream(stream);

        var maps = new List<Gbx<CGameCtnChallenge>>();
        var uidCarPairs = new HashSet<(string, string)>();

        var parseZip = false;

        try
        {
            var gbxMap = await Gbx.ParseAsync<CGameCtnChallenge>(bufferedStream);
            gbxMap.FilePath = mapfile.Filename;
            maps.Add(gbxMap);
        }
        catch (NotAGbxException)
        {
            bufferedStream.Position = 0;
            parseZip = true;
        }

        if (parseZip)
        {
            using var zip = new ZipArchive(bufferedStream, ZipArchiveMode.Read, true);

            if (zip.Entries.Count == 0)
            {
                _logger.LogDebug("ZIP file is empty.");
                await FollowupAsync("ZIP file is empty.", ephemeral: !IsBotChannel());
                return;
            }

            if (zip.Entries.Count > 10)
            {
                _logger.LogDebug("ZIP file contains too many files.");
                await FollowupAsync("ZIP file contains too many files.", ephemeral: !IsBotChannel());
                return;
            }

            foreach (var entry in zip.Entries)
            {
                if (entry.Length > 10 * 1024 * 1024)
                {
                    _logger.LogWarning("File '{MapFile}' in ZIP is too large.", entry.Name);
                    await FollowupAsync($"File '{entry.Name}' in ZIP is too large.", ephemeral: !IsBotChannel());
                    continue;
                }

                await using var entryStream = entry.Open();

                try
                {
                    var gbxMap = Gbx.Parse<CGameCtnChallenge>(entryStream);
                    gbxMap.FilePath = entry.Name;
                    maps.Add(gbxMap);
                    _logger.LogInformation("Parsed map file '{MapFile}' in ZIP.", entry.Name);
                }
                catch (NotAGbxException)
                {
                    _logger.LogWarning("Failed to parse map file '{MapFile}' in ZIP, not a Gbx.", entry.Name);
                    continue;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to parse map file '{MapFile}' in ZIP.", entry.Name);
                    continue;
                }
            }
        }

        var successfulMaps = new List<ConvertedMapModel>();
        var unsuccessfulMessages = new List<string>();

        foreach (var gbxMap in maps)
        {
            var map = gbxMap.Node;

            var deformattedMapName = TextFormatter.Deformat(map.MapName);

            if (map.ScriptMetadata is null)
            {
                _logger.LogWarning("Map '{Map}' does not contain metadata.", deformattedMapName);
                unsuccessfulMessages.Add($"Map '{deformattedMapName}' does not contain metadata.");
                continue;
            }

            var originalMapUid = map.ScriptMetadata.GetText("ENVIMIX_OriginalMapUid") ?? string.Empty;
            var car = map.ScriptMetadata.GetText("ENVIMIX_Car") ?? string.Empty;

            if (!uidCarPairs.Add((originalMapUid, car)))
            {
                _logger.LogWarning("Map '{MapFile}' is a duplicate in the ZIP, skipped.", gbxMap.FilePath);
                unsuccessfulMessages.Add($"Map '{deformattedMapName}' is a duplicate in the ZIP, skipped.");
                continue;
            }

            var convertedMap = await _db.ConvertedMaps
                .Include(x => x.Campaign)
                .FirstOrDefaultAsync(x => x.OriginalUid == originalMapUid && x.CarId == car);

            if (convertedMap is null)
            {
                _logger.LogDebug("Map '{Map}' does not exist in database.", deformattedMapName);
                unsuccessfulMessages.Add($"Map '{deformattedMapName}' does not exist in the database.");
                continue;
            }

            if (convertedMap.ClaimedById is null)
            {
                if (!await _db.Users.AnyAsync(x => x.Id == user.Id))
                {
                    _logger.LogDebug("User does not exist in the database, adding...");
                    await _db.Users.AddAsync(new UserModel { Id = user.Id });
                }

                convertedMap.ClaimedById = user.Id;
            }

            // If the track is claimed to be possible, don't allow others to validate it
            if (!convertedMap.Impossible && convertedMap.ClaimedById != user.Id)
            {
                _logger.LogWarning("User '{User}' did not claim map '{Map}'.", user.GlobalName, convertedMap.Name);
                unsuccessfulMessages.Add($"You did not claim map '{deformattedMapName}'. Please claim it first.");
                continue;
            }

            using var mapToCompareStream = new MemoryStream(convertedMap.Data);
            var mapToCompare = Gbx.ParseNode<CGameCtnChallenge>(mapToCompareStream);

            if (convertedMap.Validated)
            {
                if (map.AuthorTime >= mapToCompare.AuthorTime)
                {
                    _logger.LogInformation("Map '{Map}' is already validated.", deformattedMapName);
                    unsuccessfulMessages.Add($"Map '{deformattedMapName}' is already validated.");
                    continue;
                }

                unsuccessfulMessages.Add($"Map '{deformattedMapName}' has been improved with a new author time, but prefer to upload final validations.");
            }

            if (map.AuthorTime is null || map.AuthorTime == TimeInt32.MaxValue)
            {
                _logger.LogWarning("Map '{Map}' does not have author time.", deformattedMapName);
                unsuccessfulMessages.Add($"Map '{deformattedMapName}' does not have author time.");
                continue;
            }

            if (map.GoldTime is null || map.GoldTime == TimeInt32.MaxValue)
            {
                _logger.LogWarning("Map '{Map}' does not have gold time.", deformattedMapName);
                unsuccessfulMessages.Add($"Map '{deformattedMapName}' does not have gold time.");
                continue;
            }

            if (map.SilverTime is null || map.SilverTime == TimeInt32.MaxValue)
            {
                _logger.LogWarning("Map '{Map}' does not have silver time.", deformattedMapName);
                unsuccessfulMessages.Add($"Map '{deformattedMapName}' does not have silver time.");
                continue;
            }

            if (map.BronzeTime is null || map.BronzeTime == TimeInt32.MaxValue)
            {
                _logger.LogWarning("Map '{Map}' does not have bronze time.", deformattedMapName);
                unsuccessfulMessages.Add($"Map '{deformattedMapName}' does not have bronze time.");
                continue;
            }

            if (map.Blocks?.Count != mapToCompare.Blocks?.Count)
            {
                _logger.LogWarning("Map '{Map}' has different block count.", deformattedMapName);
                unsuccessfulMessages.Add($"Map '{deformattedMapName}' has different block count.");
                continue;
            }

            if (map.AnchoredObjects?.Count != mapToCompare.AnchoredObjects?.Count)
            {
                _logger.LogWarning("Map '{Map}' has different item count.", deformattedMapName);
                unsuccessfulMessages.Add($"Map '{deformattedMapName}' has different item count.");
                continue;
            }

            if (mapToCompare.PlayerModel is not null && mapToCompare.PlayerModel.Id != car)
            {
                _logger.LogWarning("Given map '{Map}' has different car from the car in metadata ({PlayerModelId} != {car}).", deformattedMapName, mapToCompare.PlayerModel, car);
                unsuccessfulMessages.Add($"Map '{deformattedMapName}' has different car from the car in metadata ({mapToCompare.PlayerModel.Id} != {car}).");
                continue;
            }

            if (map.PlayerModel is null)
            {
                _logger.LogWarning("Map '{Map}' player model is null.", deformattedMapName);
                unsuccessfulMessages.Add($"Map '{deformattedMapName}' player model is null.");
                continue;
            }

            if (map.PlayerModel.Id != car || map.PlayerModel.Collection.Number != 10003 || map.PlayerModel.Author != "Nadeo")
            {
                if (map.PlayerModel.Id != "" || car != "CarSport")
                {
                    _logger.LogWarning("Map '{Map}' has different player model ({PlayerModel}).", deformattedMapName, map.PlayerModel);
                    unsuccessfulMessages.Add($"Map '{deformattedMapName}' has different player model ({map.PlayerModel.Id}).");
                    continue;
                }
            }

            if (map.LightmapCacheData is null)
            {
                _logger.LogWarning("Map '{Map}' does not have lightmaps.", deformattedMapName);
                unsuccessfulMessages.Add($"Map '{deformattedMapName}' does not have lightmaps. We will accept it but prefer calculating them yourself.");
            }

            if (convertedMap.Impossible)
            {
                convertedMap.ClaimedById = user.Id;
                convertedMap.Impossible = false;
            }

            convertedMap.Validated = true;

            using var mapMs = new MemoryStream();
            gbxMap.Save(mapMs);

            convertedMap.Data = mapMs.ToArray();

            _logger.LogInformation("Map '{Map}' validated.", deformattedMapName);

            successfulMaps.Add(convertedMap);
        }

        var sbIssues = new StringBuilder("There were some issues:\n");
        foreach (var message in unsuccessfulMessages)
        {
            sbIssues.AppendLine($"- {message}");
        }
        var issues = sbIssues.ToString();

        if (successfulMaps.Count == 0)
        {
            _logger.LogWarning("No maps were validated.");
            await FollowupAsync($"No maps were validated. {issues}", ephemeral: !IsBotChannel());
            return;
        }

        await _db.SaveChangesAsync();

        var sb = new StringBuilder();

        sb.AppendLine(successfulMaps.Count == 1 ? "Map validated:" : "Maps validated:");

        foreach (var mapGrouping in successfulMaps.GroupBy(x => x.OriginalName))
        {
            sb.Append("- ");
            sb.Append(TextFormatter.Deformat(mapGrouping.Key));
            sb.Append(" - ");
            sb.AppendLine(string.Join(", ", mapGrouping.Select(x => x.CarId)));
        }

        if (unsuccessfulMessages.Count > 0)
        {
            sb.Append(issues);
        }

        sb.Append(unsuccessfulMessages.Count == 0 ? "Thanks for the contribution!" : "Thanks for the contribution though!");

        await FollowupAsync(sb.ToString(), ephemeral: !IsBotChannel());

        foreach (var map in successfulMaps.DistinctBy(x => x.CampaignId))
        {
            await _discordReporter.UpdateStatusDescriptionAsync(map.Campaign);
            await _discordReporter.CheckCampaignCompletionAsync(map.Campaign, map.CarId);
        }
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
}
