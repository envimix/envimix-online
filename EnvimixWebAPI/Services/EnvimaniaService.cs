using EnvimixWebAPI.Entities;
using EnvimixWebAPI.Extensions;
using EnvimixWebAPI.Models;
using EnvimixWebAPI.Models.Envimania;
using EnvimixWebAPI.Security;
using GBX.NET;
using GBX.NET.Engines.Game;
using ManiaAPI.ManiaPlanetAPI;
using ManiaAPI.Xml.MP4;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Caching.Hybrid;
using OneOf;
using System.Data;
using System.Security.Claims;
using System.Xml.Linq;
using TmEssentials;

namespace EnvimixWebAPI.Services;

public interface IEnvimaniaService
{
    Task<OneOf<EnvimaniaServer, ValidationFailureResponse, ActionUnprocessableResponse, ActionForbiddenResponse>>
        RegisterAsync(EnvimaniaRegistrationRequest registerRequest, ClaimsPrincipal principal, CancellationToken cancellationToken);

    Task<OneOf<EnvimaniaBanResponse, ValidationFailureResponse, ActionUnprocessableResponse>>
        BanAsync(EnvimaniaBanRequest request, ClaimsPrincipal principal, CancellationToken cancellationToken);

    Task<OneOf<EnvimaniaUnbanResponse, ValidationFailureResponse, ActionUnprocessableResponse>>
        UnbanAsync(EnvimaniaUnbanRequest unbanRequest, ClaimsPrincipal principal, CancellationToken cancellationToken);

    Task<OneOf<EnvimaniaSessionResponse, ValidationFailureResponse, ActionForbiddenResponse>>
        CreateSessionAsync(EnvimaniaSessionRequest request, CancellationToken cancellationToken);

    Task<OneOf<EnvimaniaSessionStatusResponse, ActionForbiddenResponse>>
        CheckSessionStatusAsync(ClaimsPrincipal principal, CancellationToken cancellationToken);

    Task<OneOf<EnvimaniaSessionRecordResponse, ValidationFailureResponse, ActionForbiddenResponse>>
        SetSessionRecordAsync(EnvimaniaSessionRecordRequest request, ClaimsPrincipal principal, CancellationToken cancellationToken);

    Task<OneOf<bool, ValidationFailureResponse, ActionForbiddenResponse>>
        SetRecordAsync(HttpRequest request, ClaimsPrincipal principal, CancellationToken cancellationToken);

    Task<OneOf<EnvimaniaSessionRecordResponse, ValidationFailureResponse, ActionForbiddenResponse>>
        SetSessionRecordsAsync(EnvimaniaSessionRecordBulkRequest request, ClaimsPrincipal principal, CancellationToken cancellationToken);

    Task<OneOf<EnvimaniaRecordsResponse, ActionForbiddenResponse>>
        GetSessionRecordsAsync(EnvimaniaRecordFilter filter, ClaimsPrincipal principal, CancellationToken cancellationToken);

    Task<OneOf<EnvimaniaSessionClosedResponse, ActionForbiddenResponse>>
        CloseSessionAsync(ClaimsPrincipal principal, CancellationToken cancellationToken);

    Task<OneOf<EnvimaniaRecordsResponse, ValidationFailureResponse>>
        GetRecordsAsync(string mapUid, EnvimaniaRecordFilter filter, string zone, CancellationToken cancellationToken);

    Task<OneOf<EnvimaniaSessionUser, ActionForbiddenResponse>>
        GetSessionUserAdditionalInfoAsync(UserInfo user, ClaimsPrincipal principal, CancellationToken cancellationToken);

    Task<OneOf<List<EnvimaniaSessionUser>, ValidationFailureResponse, ActionForbiddenResponse>>
        GetSessionUsersAdditionalInfoAsync(IDictionary<string, UserInfo> userInfos, ClaimsPrincipal principal, CancellationToken cancellationToken);

    Task<List<RecordEntity>> GetValidationsAsync(string mapUid, CancellationToken cancellationToken);
}

public sealed class EnvimaniaService(
    AppDbContext db,
    HybridCache cache,
    MasterServerMP4 masterServer,
    ManiaPlanetIngameAPI mpIngameApi,
    IMapService mapService,
    IUserService userService,
    IZoneService zoneService,
    IModService modService,
    IRatingService ratingService,
    ITokenService tokenService,
    ILogger<EnvimaniaService> logger) : IEnvimaniaService
{
    public async Task<OneOf<EnvimaniaServer, ValidationFailureResponse, ActionUnprocessableResponse, ActionForbiddenResponse>> RegisterAsync(
        EnvimaniaRegistrationRequest request,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        // VALIDATION START

        logger.LogInformation("Attempt to register dedicated server {serverLogin}...", request.ServerLogin);

        if (!Validator.ValidateLogin(request.ServerLogin))
        {
            return new ValidationFailureResponse("Invalid server login");
        }

        cancellationToken.ThrowIfCancellationRequested();

        // validate with ManiaPlanet web services here, so that only owner can register the server login

        var server = await db.Servers.FirstOrDefaultAsync(x => x.Id == request.ServerLogin, cancellationToken);

        if (server is not null)
        {
            return new ActionUnprocessableResponse("Server login already registered");
        }

        // Check for server ownership (skipped for super admins)
        if (!principal.IsInRole(Roles.SuperAdmin))
        {
            if (string.IsNullOrWhiteSpace(request.ServerToken))
            {
                return new ActionForbiddenResponse("Server registering from principals is not yet supported");

                /*var userOwnsServerLogin = await UserOwnsServerLoginAsync(request.ServerLogin, principal, cancellationToken);

                if (!userOwnsServerLogin)
                {
                    return new ActionForbiddenResponse("Server login not owned by user");
                }*/
            }

            var ingameAuthResult = await mpIngameApi.AuthenticateAsync(request.ServerLogin, request.ServerToken, cancellationToken);

            if (ingameAuthResult.Login != request.ServerLogin)
            {
                return new ActionForbiddenResponse("Invalid server token");
            }

            logger.LogDebug("Server ownership of {serverLogin} verified.", request.ServerLogin);
        }

        // VALIDATION END

        server = new ServerEntity
        {
            Id = request.ServerLogin
        };

        await db.Servers.AddAsync(server, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Dedicated server {serverLogin} has been registered.", server.Id);

        return new EnvimaniaServer
        {
            ServerLogin = server.Id
        };
    }

    /*private async Task<bool> UserOwnsServerLoginAsync(string serverLogin, ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var accessToken = principal.FindFirstValue("WebServicesToken") ?? throw new Exception("AccessToken is null");

        var http = httpFactory.CreateClient(Consts.ManiaPlanetWebServices);

        using var request = new HttpRequestMessage(HttpMethod.Get, "https://maniaplanet.com/webservices/me/dedicated");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await http.SendAsync(request, cancellationToken);

        var serverIsOwned = false;

        await foreach (var server in response.Content.ReadFromJsonAsAsyncEnumerable(AppJsonSerializerContext.Default.ManiaPlanetDedicatedServer, cancellationToken))
        {
            if (server?.Login == serverLogin)
            {
                serverIsOwned = true;
                logger.LogDebug("{user} owns dedicated server '{serverLogin}'", principal.GetName(), serverLogin);
                break;
            }
        }

        return serverIsOwned;
    }*/

    public async Task<OneOf<EnvimaniaBanResponse, ValidationFailureResponse, ActionUnprocessableResponse>> BanAsync(
        EnvimaniaBanRequest request, ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        // VALIDATION START

        logger.LogInformation("Attempt to ban dedicated server {serverLogin} by {user}...",
            request.ServerLogin, principal.GetName());

        if (!Validator.ValidateLogin(request.ServerLogin))
        {
            return new ValidationFailureResponse("Invalid server login");
        }

        var server = await db.Servers.FirstOrDefaultAsync(x => x.Id == request.ServerLogin, cancellationToken);

        if (server is null)
        {
            return new ValidationFailureResponse("Server login not registered");
        }

        if (server.BanReason is not null)
        {
            return new ActionUnprocessableResponse("Server login already banned");
        }

        // VALIDATION END

        // Empty string = unspecified reason
        server.BanReason = request.Reason ?? string.Empty;
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Dedicated server {serverLogin} has been banned.", server.Id);

        return new EnvimaniaBanResponse
        {
            ServerLogin = server.Id,
            Banned = server.BanReason is not null,
            Reason = server.BanReason ?? ""
        };
    }

    public async Task<OneOf<EnvimaniaUnbanResponse, ValidationFailureResponse, ActionUnprocessableResponse>> UnbanAsync(
        EnvimaniaUnbanRequest request, ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        logger.LogInformation("Attempt to unban dedicated server {serverLogin} by {user}...",
            request.ServerLogin, principal.GetName());

        // VALIDATION START

        if (!Validator.ValidateLogin(request.ServerLogin))
        {
            return new ValidationFailureResponse("Invalid server login");
        }

        var server = await db.Servers.FirstOrDefaultAsync(x => x.Id == request.ServerLogin, cancellationToken);

        if (server is null)
        {
            return new ValidationFailureResponse("Server login not registered");
        }

        if (server.BanReason is null)
        {
            return new ActionUnprocessableResponse("Server login not banned");
        }

        // VALIDATION END

        server.BanReason = null;

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Dedicated server {serverLogin} has been unbanned.", server.Id);

        return new EnvimaniaUnbanResponse
        {
            ServerLogin = server.Id
        };
    }

    public async Task<OneOf<EnvimaniaSessionResponse, ValidationFailureResponse, ActionForbiddenResponse>> CreateSessionAsync(
        EnvimaniaSessionRequest request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Attempt to create a new session via {user} dedicated server...", request.ServerLogin);

        // VALIDATION START

        foreach (var car in request.Cars)
        {
            if (!modService.IsValidCar(car))
            {
                return new ValidationFailureResponse($"Invalid car ({car})");
            }
        }

        var server = await db.Servers.FirstOrDefaultAsync(x => x.Id == request.ServerLogin, cancellationToken);

        if (server is null)
        {
            return new ValidationFailureResponse("Server login not registered");
        }

        if (server.BanReason is not null)
        {
            return ActionForbiddenResponse.ServerLoginBanned;
        }

        // VALIDATION END

        // can throw 403
        var ingameAuthResult = await mpIngameApi.AuthenticateAsync(request.ServerLogin, request.ServerToken, cancellationToken);

        if (ingameAuthResult.Login != request.ServerLogin)
        {
            return new ValidationFailureResponse("Invalid server token");
        }

        logger.LogDebug("Generating new session token...");

        var sessionGuid = Guid.CreateVersion7();

        var token = tokenService.GenerateEnvimaniaSessionToken(sessionGuid, request.Map.Uid, server.Id, out var startedAt, out var expiresAt);

        logger.LogInformation("Adding/updating map {mapName}...", TextFormatter.Deformat(request.Map.Name));

        var map = await mapService.GetAddOrUpdateAsync(request.Map, server, cancellationToken: cancellationToken);

        var session = new EnvimaniaSessionEntity
        {
            Id = sessionGuid,
            Map = map,
            Server = server,
            StartedAt = startedAt
        };

        logger.LogDebug("Storing the Envimania session and token...");

        await db.EnvimaniaSessions.AddAsync(session, cancellationToken);

        /*await db.EnvimaniaSessionTokens.AddAsync(new EnvimaniaSessionTokenEntity
        {
            Id = token,
            Session = session,
            ExpiresAt = expiresAt
        }, cancellationToken);*/

        logger.LogInformation("Updating user information of {playerCount} players...", request.Players.Length);

        await userService.GetAddOrUpdateMultipleAsync(request.Players, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Session on {serverLogin} created. Expires at {expiresAt}.", request.ServerLogin, expiresAt);

        logger.LogDebug("Retrieving general ratings of the modifications...");

        var ratings = new List<FilteredRating>();

        foreach (var car in request.Cars)
        {
            var filter = new RatingFilter()
            {
                Car = car
            };

            var rating = await ratingService.GetAverageAsync(map.Id, filter, cancellationToken);

            ratings.Add(new()
            {
                Filter = filter,
                Rating = rating with
                {
                    Difficulty = rating.Difficulty is null ? -1 : rating.Difficulty,
                    Quality = rating.Quality is null ? -1 : rating.Quality
                }
            });
        }

        logger.LogDebug("General ratings retrieved. Retrieving ratings of {playerCount} players...", request.Players.Length);

        var logins = request.Players
            .Select(x => x.Login)
            .ToHashSet();

        var userRatings = await ratingService.GetByUserLoginsAsync(map.Id, logins, cancellationToken);

        foreach (var (login, filteredRatings) in userRatings)
        {
            foreach (var rating in filteredRatings)
            {
                rating.Rating = rating.Rating with
                {
                    Difficulty = rating.Rating.Difficulty is null ? -1 : rating.Rating.Difficulty,
                    Quality = rating.Rating.Quality is null ? -1 : rating.Rating.Quality
                };
            }
        }

        logger.LogDebug("Ratings of players on the current server retrieved.");

        var validations = await GetValidationsAsync(map.Id, cancellationToken);

        return new EnvimaniaSessionResponse
        {
            ServerLogin = request.ServerLogin,
            SessionToken = token,
            Ratings = ratings,
            UserRatings = userRatings,
            Validations = validations.ToDictionary(x => $"{x.Car.Id}_{x.Gravity}_{x.Laps}", rec => new EnvimaniaRecordInfo
            {
                User = new UserInfo
                {
                    Login = rec.User.Id,
                    Nickname = rec.User.Nickname ?? "",
                    Zone = rec.User.Zone?.Name ?? "",
                    AvatarUrl = rec.User.AvatarUrl ?? "",
                    Language = rec.User.Language ?? "",
                    Description = rec.User.Language ?? "",
                    Color = rec.User.Color ?? [-1, -1, -1],
                    SteamUserId = rec.User.SteamUserId ?? "",
                    FameStars = rec.User.FameStars ?? 0,
                    LadderPoints = rec.User.LadderPoints ?? 0,
                },
                Time = rec.Checkpoints.Last().Time,
                Score = rec.Checkpoints.Last().Score,
                NbRespawns = rec.Checkpoints.Last().NbRespawns,
                Distance = rec.Checkpoints.Last().Distance,
                Speed = rec.Checkpoints.Last().Speed,
                Verified = true,
                Projected = false,
                GhostUrl = "" // TODO: read from DB
            })
        };
    }

    public async Task<OneOf<EnvimaniaSessionClosedResponse, ActionForbiddenResponse>> CloseSessionAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        logger.LogInformation("Attempt to close a session via {user} dedicated server...", principal.GetName());

        // VALIDATION START

        var banReason = principal.FindFirstValue("BanReason");

        if (banReason is not null)
        {
            return ActionForbiddenResponse.ServerLoginBanned;
        }

        // VALIDATION END
        var sessionGuid = Guid.Parse(principal.FindFirstValue(EnvimaniaClaimTypes.SessionGuid) ?? throw new Exception("Session GUID is null"));

        var session = await db.EnvimaniaSessions
            .FirstOrDefaultAsync(x => x.Id == sessionGuid, cancellationToken)
            ?? throw new Exception("Session not found in database but should have been found");

        var sessionEnd = DateTimeOffset.UtcNow;

        session.FinishedGracefully = true;
        session.EndedAt = sessionEnd;

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Session closed gracefully at {sessionEnd}.", sessionEnd);

        return new EnvimaniaSessionClosedResponse();
    }

    public async Task<OneOf<EnvimaniaSessionStatusResponse, ActionForbiddenResponse>> CheckSessionStatusAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        // VALIDATION START

        var banReason = principal.FindFirstValue("BanReason");

        if (banReason is not null)
        {
            return ActionForbiddenResponse.ServerLoginBanned;
        }

        // VALIDATION END

        logger.LogInformation("Session status checked");

        return new EnvimaniaSessionStatusResponse();
    }

    public async Task<OneOf<EnvimaniaSessionRecordResponse, ValidationFailureResponse, ActionForbiddenResponse>> SetSessionRecordAsync(EnvimaniaSessionRecordRequest request, ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        logger.LogInformation("Attempt to set a record in a session via {user} dedicated server...", principal.GetName());

        // VALIDATION START

        var banReason = principal.FindFirstValue("BanReason");

        if (banReason is not null)
        {
            return ActionForbiddenResponse.ServerLoginBanned;
        }

        if (await ValidateRecordRequestAsync(request, cancellationToken) is string msg)
        {
            return new ValidationFailureResponse(msg);
        }

        // VALIDATION END

        if (await AddRecordAsync(request, principal, cancellationToken) is string msgAfter)
        {
            return new ValidationFailureResponse(msgAfter);
        }

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Record set successfully.");

        // Get updated record list
        var mapUid = principal.FindFirstValue(EnvimaniaClaimTypes.SessionMapUid) ?? throw new Exception("Session MapUid is null");

        var filter = new EnvimaniaRecordFilter()
        {
            Car = request.Car,
            Gravity = request.Gravity,
            Laps = request.Laps
        };

        logger.LogDebug("Retrieving updated records...");
        var recs = await GetRecordsWithoutValidationAsync(mapUid, filter, "World", cancellationToken);

        logger.LogDebug("Updated records retrieved.");

        return new EnvimaniaSessionRecordResponse
        {
            UpdatedRecords = [recs]
        };
    }

    public async Task<OneOf<bool, ValidationFailureResponse, ActionForbiddenResponse>> SetRecordAsync(HttpRequest request, ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        logger.LogInformation("Attempt to set a record via {user} user...", principal.GetName());

        // VALIDATION START

        var banReason = principal.FindFirstValue("BanReason");

        if (banReason is not null)
        {
            return ActionForbiddenResponse.UserLoginBanned;
        }

        // VALIDATION END

        var serverTimestamp = DateTimeOffset.UtcNow;
        var timestamp = serverTimestamp;

        using var ms = new MemoryStream();
        await request.Body.CopyToAsync(ms, CancellationToken.None);

        var timestampSuggestedByUser = default(DateTimeOffset?);
        if (request.Headers.TryGetValue("X-Envimix-Timestamp", out var tsValues) && long.TryParse(tsValues.FirstOrDefault(), out var tsLong))
        {
            timestampSuggestedByUser = DateTimeOffset.FromUnixTimeSeconds(tsLong);

            if ((serverTimestamp - timestampSuggestedByUser.Value).Duration() < TimeSpan.FromSeconds(2))
            {
                timestamp = timestampSuggestedByUser.Value;
            }
        }

        ms.Position = 0;

        var ghost = await Gbx.ParseNodeAsync<CGameCtnGhost>(ms, cancellationToken: CancellationToken.None);

        var carName = ghost.PlayerModel?.Id switch
        {
            "CanyonCar" or "Vehicles\\CanyonCar.Item.Gbx" or "Vehicles\\CanyonCarTurbo.Item.Gbx" => "CanyonCar",
            "StadiumCar" or "Vehicles\\StadiumCar.Item.Gbx" or "Vehicles\\StadiumCarTurbo.Item.Gbx" => "StadiumCar",
            "ValleyCar" or "Vehicles\\ValleyCar.Item.Gbx" or "Vehicles\\ValleyCarTurbo.Item.Gbx" => "ValleyCar",
            "LagoonCar" or "Vehicles\\LagoonCar.Item.Gbx" or "Vehicles\\LagoonCarTurbo.Item.Gbx" => "LagoonCar",
            "Vehicles\\TrafficCar.Item.Gbx" => "TrafficCar",
            "Vehicles\\DesertCar.Item.Gbx" => "DesertCar",
            "Vehicles\\RallyCar.Item.Gbx" => "RallyCar",
            "Vehicles\\SnowCar.Item.Gbx" => "SnowCar",
            "Vehicles\\IslandCar.Item.Gbx" => "IslandCar",
            "Vehicles\\BayCar.Item.Gbx" => "BayCar",
            "Vehicles\\CoastCar.Item.Gbx" => "CoastCar",
            _ => null
        };

        if (carName is null)
        {
            return new ValidationFailureResponse("Invalid vehicle");
        }

        var ghostRawData = ms.ToArray();

        var userModel = await userService.GetAsync(principal.GetName(), CancellationToken.None);

        if (userModel is null)
        {
            return new ValidationFailureResponse("User not found");
        }

        if (string.IsNullOrWhiteSpace(ghost.Validate_ChallengeUid))
        {
            return new ValidationFailureResponse("Invalid map UID in ghost");
        }

        if (string.IsNullOrWhiteSpace(ghost.Validate_TitleId))
        {
            return new ValidationFailureResponse("Invalid title ID in ghost");
        }

        if (ghost.RaceTime is null)
        {
            return new ValidationFailureResponse("Invalid race time in ghost");
        }

        if (string.IsNullOrWhiteSpace(ghost.Validate_RaceSettings))
        {
            return new ValidationFailureResponse("Invalid race settings in ghost");
        }

        var raceXml = XDocument.Parse($"<root>{ghost.Validate_RaceSettings}</root>");
        var laps = (int?)raceXml.Descendants("laps").FirstOrDefault() ?? 0;

        var map = await mapService.GetAddOrUpdateAsync(ghost.Validate_ChallengeUid, ghost.Validate_TitleId, CancellationToken.None);

        var car = await modService.GetOrAddCarAsync(carName, cancellationToken);

        var gravity = 0; // TODO should be configurable

        var bestLastCheckpointsQueryable = db.Records
            .Include(x => x.User)
            .Include(x => x.Car)
            .Include(x => x.Map)
            .Where(x => x.User == userModel
                && x.Map == map
                && x.Car == car
                && x.Gravity == gravity
                && x.Laps == laps)
            .Select(x => x.Checkpoints.OrderBy(x => x.Time).Last());

        var newRecord = new EnvimaniaRecord
        {
            Time = ghost.RaceTime.Value.TotalMilliseconds,
            Score = ghost.StuntScore ?? 0,
            NbRespawns = ghost.Respawns ?? -1,
            Speed = -1,
            Distance = -1
        };

        var isPb = await IsRecordPersonalBestAsync(bestLastCheckpointsQueryable, newRecord, cancellationToken);

        if (!isPb)
        {
            return new ValidationFailureResponse("Invalid record");
        }

        var record = new RecordEntity
        {
            User = userModel,
            Map = map,
            Car = car,
            Gravity = gravity,
            DrivenAt = timestamp, // + request.PreferenceNumber
            ServersideDrivenAt = serverTimestamp,
            SessionId = null,
            Laps = laps,
            Ghost = new GhostEntity { Data = ghostRawData }
        };

        foreach (var cp in ghost.Checkpoints ?? [])
        {
            record.Checkpoints.Add(new CheckpointEntity
            {
                Record = record,
                Time = cp.Time.GetValueOrDefault().TotalMilliseconds,
                Score = cp.StuntsScore ?? 0,
                NbRespawns = ghost.Respawns ?? -1, // weird stuff
                Distance = -1,
                Speed = -1
            });
        }

        await db.Records.AddAsync(record, cancellationToken);

        return await db.SaveChangesAsync(cancellationToken) > 0;
    }

    public async Task<OneOf<EnvimaniaSessionRecordResponse, ValidationFailureResponse, ActionForbiddenResponse>> SetSessionRecordsAsync(EnvimaniaSessionRecordBulkRequest request, ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        logger.LogInformation("Attempt to set records in a session via {user} dedicated server...", principal.GetName());

        // VALIDATION START

        var banReason = principal.FindFirstValue("BanReason");

        if (banReason is not null)
        {
            return ActionForbiddenResponse.ServerLoginBanned;
        }

        if (request.Requests.Length > 20)
        {
            return new ValidationFailureResponse("Maximum record count exceeded");
        }

        var allowedRequests = new List<EnvimaniaSessionRecordRequest>();

        foreach (var r in request.Requests)
        {
            if (await ValidateRecordRequestAsync(r, cancellationToken) is null)
            {
                allowedRequests.Add(r);
            }
        }

        // VALIDATION END

        var filters = new List<EnvimaniaRecordFilter>();

        foreach (var r in allowedRequests)
        {
            await AddRecordAsync(r, principal, cancellationToken);

            filters.Add(new() { Car = r.Car, Gravity = r.Gravity });
        }

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("{recCount} records set successfully.", allowedRequests.Count);

        // Get updated record list
        var mapUid = principal.FindFirstValue(EnvimaniaClaimTypes.SessionMapUid) ?? throw new Exception("Session MapUid is null");

        var updatedRecs = new List<EnvimaniaRecordsResponse>();

        logger.LogDebug("Retrieving updated records...");

        foreach (var f in filters)
        {
            updatedRecs.Add(await GetRecordsWithoutValidationAsync(mapUid, f, "World", cancellationToken));
        }

        logger.LogDebug("Updated records retrieved.");

        return new EnvimaniaSessionRecordResponse
        {
            UpdatedRecords = updatedRecs
        };
    }

    private async Task<string?> ValidateRecordRequestAsync(EnvimaniaSessionRecordRequest request, CancellationToken cancellationToken)
    {
        if (!Validator.ValidateLogin(request.User.Login))
        {
            return "Invalid login";
        }

        if (!Validator.ValidateNickname(request.User.Nickname))
        {
            return "Invalid Nickname";
        }

        if (!await zoneService.IsValidAsync(request.User.Zone, cancellationToken))
        {
            return "Invalid Zone";
        }

        if (request.Record.Checkpoints.Length == 0)
        {
            return "Invalid record";
        }

        var finishCp = request.Record.Checkpoints[^1];

        if (request.Record.Time != finishCp.Time
            || request.Record.Score != finishCp.Score
            || request.Record.NbRespawns != finishCp.NbRespawns
            || request.Record.Distance != finishCp.Distance
            || request.Record.Speed != finishCp.Speed)
        {
            return "Invalid record";
        }

        if (!modService.IsValidCar(request.Car))
        {
            return "Invalid Car";
        }

        if (!modService.IsValidGravity(request.Gravity))
        {
            return "Invalid Gravity";
        }

        return null;
    }

    private async Task<string?> AddRecordAsync(EnvimaniaSessionRecordRequest request, ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var userModel = await userService.GetAddOrUpdateAsync(request.User, cancellationToken);

        var sessionGuid = Guid.Parse(principal.FindFirstValue(EnvimaniaClaimTypes.SessionGuid) ?? throw new Exception("Session GUID is null"));
        var mapUid = principal.FindFirstValue(EnvimaniaClaimTypes.SessionMapUid) ?? throw new Exception("Session MapUid is null");

        var map = await mapService.GetAsync(mapUid, cancellationToken)
            ?? throw new Exception("Map not found but should have been found");

        var car = await modService.GetOrAddCarAsync(request.Car, cancellationToken);

        var gravity = request.Gravity;
        var laps = request.Laps;

        var bestLastCheckpointsQueryable = db.Records
            .Include(x => x.User)
            .Include(x => x.Car)
            .Include(x => x.Map)
            .Where(x => x.User == userModel
                && x.Map == map
                && x.Car == car
                && x.Gravity == gravity
                && x.Laps == laps)
            .Select(x => x.Checkpoints.OrderBy(x => x.Time).Last());

        var isPb = await IsRecordPersonalBestAsync(bestLastCheckpointsQueryable, request.Record, cancellationToken);

        if (!isPb)
        {
            return "Invalid record";
        }

        var record = new RecordEntity
        {
            User = userModel,
            Map = map,
            Car = car,
            Gravity = gravity,
            DrivenAt = DateTimeOffset.UtcNow, // + request.PreferenceNumber
            SessionId = sessionGuid,
            Laps = request.Laps
        };

        foreach (var cp in request.Record.Checkpoints)
        {
            record.Checkpoints.Add(new CheckpointEntity
            {
                Record = record,
                Time = cp.Time,
                Score = cp.Score,
                NbRespawns = cp.NbRespawns,
                Distance = cp.Distance,
                Speed = cp.Speed
            });
        }

        await db.Records.AddAsync(record, cancellationToken);

        return null;
    }

    private static async Task<bool> IsRecordPersonalBestAsync(IQueryable<CheckpointEntity> bestLastCheckpointsQueryable, EnvimaniaRecord requestRecord, CancellationToken cancellationToken)
    {
        var bestTimeLastCheckpoint = await bestLastCheckpointsQueryable
            .OrderBy(x => x.Time)
            .FirstOrDefaultAsync(cancellationToken);

        if (bestTimeLastCheckpoint is null || requestRecord.Time < bestTimeLastCheckpoint.Time)
        {
            return true;
        }

        var bestDistanceRecord = await bestLastCheckpointsQueryable
            .OrderBy(x => x.Distance)
            .FirstOrDefaultAsync(cancellationToken) ?? throw new Exception("Best record should not be null here");

        if (requestRecord.Distance < bestDistanceRecord.Distance)
        {
            return true;
        }

        var bestScoreRecord = await bestLastCheckpointsQueryable
            .OrderByDescending(x => x.Score)
            .FirstOrDefaultAsync(cancellationToken) ?? throw new Exception("Best record should not be null here");

        if (requestRecord.Score > bestScoreRecord.Score)
        {
            return true;
        }

        var bestSpeedRecord = await bestLastCheckpointsQueryable
            .OrderByDescending(x => x.Speed)
            .FirstOrDefaultAsync(cancellationToken) ?? throw new Exception("Best record should not be null here");

        if (requestRecord.Speed > bestSpeedRecord.Speed)
        {
            return true;
        }

        var bestNbRespawnsRecord = await bestLastCheckpointsQueryable
            .OrderBy(x => x.NbRespawns)
            .FirstOrDefaultAsync(cancellationToken) ?? throw new Exception("Best record should not be null here");

        if (requestRecord.NbRespawns < bestNbRespawnsRecord.NbRespawns)
        {
            return true;
        }

        return false;
    }

    public async Task<OneOf<EnvimaniaRecordsResponse, ActionForbiddenResponse>> GetSessionRecordsAsync(EnvimaniaRecordFilter filter, ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        // VALIDATION START

        var banReason = principal.FindFirstValue("BanReason");

        if (banReason is not null)
        {
            return ActionForbiddenResponse.ServerLoginBanned;
        }

        // VALIDATION END

        var mapUid = principal.FindFirstValue(EnvimaniaClaimTypes.SessionMapUid) ?? throw new Exception("Session MapUid is null");

        return await GetRecordsWithoutValidationAsync(mapUid, filter, "World", cancellationToken);
    }

    public async Task<OneOf<EnvimaniaRecordsResponse, ValidationFailureResponse>> GetRecordsAsync(string mapUid, EnvimaniaRecordFilter filter, string zone, CancellationToken cancellationToken)
    {
        // VALIDATION START

        ArgumentNullException.ThrowIfNull(mapUid);

        if (!Validator.ValidateMapUid(mapUid))
        {
            return new ValidationFailureResponse("Invalid MapUid");
        }

        // VALIDATION END

        return await GetRecordsWithoutValidationAsync(mapUid, filter, zone, cancellationToken);
    }

    private async Task<EnvimaniaRecordsResponse> GetRecordsWithoutValidationAsync(string mapUid, EnvimaniaRecordFilter filter, string zone, CancellationToken cancellationToken)
    {
        var envimaniaRecords = new List<EnvimaniaRecordInfo>();

        // get records from category combinations

        var allRecords = await db.Records
            .Include(x => x.Map)
            .Include(x => x.User)
                .ThenInclude(x => x.Zone)
            .Include(x => x.Checkpoints.OrderByDescending(x => x.Time).Take(1))
            .Where(x => x.MapId == mapUid
                && x.CarId == filter.Car
                && x.Gravity == filter.Gravity
                && x.User.Zone!.Name.StartsWith(zone)
                && x.Checkpoints.Any())
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var filteredRecords = allRecords
            .GroupBy(x => x.User.Id)
            .Select(g => g
                .OrderBy(x => x.Checkpoints.Last().Time)
                .ThenByDescending(x => x.Checkpoints.Last().Distance)
                .First())
            .OrderBy(x => x.Checkpoints.Last().Time)
            .ThenByDescending(x => x.Checkpoints.Last().Distance)
            .Take(20)
            .ToList();

        foreach (var rec in filteredRecords)
        {
            envimaniaRecords.Add(new EnvimaniaRecordInfo
            {
                User = new UserInfo
                {
                    Login = rec.User.Id,
                    Nickname = rec.User.Nickname ?? "",
                    Zone = rec.User.Zone?.Name ?? "",
                    AvatarUrl = rec.User.AvatarUrl ?? "",
                    Language = rec.User.Language ?? "",
                    Description = rec.User.Language ?? "",
                    Color = rec.User.Color ?? [-1, -1, -1],
                    SteamUserId = rec.User.SteamUserId ?? "",
                    FameStars = rec.User.FameStars ?? 0,
                    LadderPoints = rec.User.LadderPoints ?? 0,
                },
                Time = rec.Checkpoints.Last().Time,
                Score = rec.Checkpoints.Last().Score,
                NbRespawns = rec.Checkpoints.Last().NbRespawns,
                Distance = rec.Checkpoints.Last().Distance,
                Speed = rec.Checkpoints.Last().Speed,
                Verified = true,
                Projected = false,
                GhostUrl = "" // TODO: read from DB
            });
        }

        if (filteredRecords.Count == 0 || filteredRecords.First().Map.TitlePackId != "Nadeo_Envimix@bigbang1112")
        {
            return new EnvimaniaRecordsResponse
            {
                Filter = filter,
                Zone = zone,
                Records = envimaniaRecords
            };
        }

        var mpRecords = await cache.GetOrCreateAsync(CacheHelper.GetOfficialRecordsKey(mapUid, filter.Car, zone), async token =>
        {
            return await masterServer.GetMapLeaderBoardAsync("Nadeo_Envimix@bigbang1112", mapUid, count: 20, offset: 0, zone, $"{filter.Car}2", token);
        }, new() { Expiration = TimeSpan.FromMinutes(10) }, cancellationToken: cancellationToken);

        if (mpRecords.Count > 0)
        {
            var alreadyKnownUserDict = envimaniaRecords.ToDictionary(x => x.User.Login, x => x.User);

            foreach (var rec in mpRecords)
            {
                var userInfo = alreadyKnownUserDict.GetValueOrDefault(rec.Login);
                
                if (userInfo is null)
                {
                    userInfo = new UserInfo
                    {
                        Login = rec.Login,
                        Nickname = rec.Nickname,
                        Zone = "",
                        AvatarUrl = "",
                        Language = "",
                        Description = "",
                        Color = [-1, -1, -1],
                        SteamUserId = "",
                        FameStars = -1,
                        LadderPoints = -1,
                    };
                }

                envimaniaRecords.Add(new EnvimaniaRecordInfo()
                {
                    User = userInfo,
                    Time = rec.Score.TotalMilliseconds,
                    Score = -1,
                    NbRespawns = -1,
                    Distance = -1,
                    Speed = -1,
                    Verified = true,
                    Projected = true,
                    GhostUrl = rec.DownloadUrl
                });
            }

            envimaniaRecords = envimaniaRecords
                .OrderBy(y => y.Time)
                    .ThenBy(y => y.Distance)
                .DistinctBy(x => x.User.Login)
                .Take(20)
                .ToList();
        }

        return new EnvimaniaRecordsResponse
        {
            Filter = filter,
            Zone = zone,
            Records = envimaniaRecords
        };
    }

    public async Task<OneOf<EnvimaniaSessionUser, ActionForbiddenResponse>> GetSessionUserAdditionalInfoAsync(UserInfo userInfo, ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        // VALIDATION START

        var banReason = principal.FindFirstValue("BanReason");

        if (banReason is not null)
        {
            return ActionForbiddenResponse.ServerLoginBanned;
        }

        // VALIDATION END

        await userService.GetAddOrUpdateAsync(userInfo, cancellationToken);

        var mapUid = principal.FindFirstValue(EnvimaniaClaimTypes.SessionMapUid) ?? throw new Exception("Session MapUid is null");

        return new EnvimaniaSessionUser
        {
            Login = userInfo.Login,
            Ratings = await ratingService.GetByUserLoginAsync(mapUid, userInfo.Login, cancellationToken)
        };
    }

    public async Task<OneOf<List<EnvimaniaSessionUser>, ValidationFailureResponse, ActionForbiddenResponse>> GetSessionUsersAdditionalInfoAsync(IDictionary<string, UserInfo> userInfos, ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        logger.LogInformation("Attempt to get additional user information via {user} dedicated server...", principal.GetName());

        // VALIDATION START

        var banReason = principal.FindFirstValue("BanReason");

        if (banReason is not null)
        {
            return ActionForbiddenResponse.ServerLoginBanned;
        }

        foreach (var (login, userInfo) in userInfos)
        {
            if (login != userInfo.Login)
            {
                return new ValidationFailureResponse("Login does not match");
            }
        }

        // VALIDATION END

        await userService.GetAddOrUpdateMultipleAsync(userInfos.Values, cancellationToken);

        logger.LogInformation("{userCount} users have been updated.", userInfos.Values.Count);

        var mapUid = principal.FindFirstValue(EnvimaniaClaimTypes.SessionMapUid) ?? throw new Exception("Session MapUid is null");

        var ratings = await ratingService.GetByUserLoginsAsync(mapUid, userInfos.Keys, cancellationToken);

        // Mapping rating nulls to -1s for ManiaScript
        foreach (var (_, filteredRatings) in ratings)
        {
            foreach (var fRating in filteredRatings)
            {
                fRating.Rating = fRating.Rating with
                {
                    Difficulty = fRating.Rating.Difficulty is null ? -1 : fRating.Rating.Difficulty,
                    Quality = fRating.Rating.Quality is null ? -1 : fRating.Rating.Quality
                };
            }
        }

        var users = new List<EnvimaniaSessionUser>();

        foreach (var userInfo in userInfos.Values)
        {
            var user = new EnvimaniaSessionUser
            {
                Login = userInfo.Login,
                Ratings = ratings.GetValueOrDefault(userInfo.Login) ?? []
            };

            users.Add(user);
        }

        return users;
    }

    public async Task<List<RecordEntity>> GetValidationsAsync(string mapUid, CancellationToken cancellationToken)
    {
        return await db.Records
            .Include(x => x.User)
            .Include(x => x.Car)
            .Include(x => x.Map)
            .Include(x => x.Checkpoints)
            .Where(x => x.Map.Id == mapUid)
            .GroupBy(x => new { x.Car.Id, x.Gravity, x.Laps })
            .Select(g => g.OrderBy(x => x.DrivenAt).First())
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}
