using EnvimixWebAPI.Dtos;
using EnvimixWebAPI.Entities;
using EnvimixWebAPI.Models;
using ManiaAPI.ManiaPlanetAPI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using OneOf;

namespace EnvimixWebAPI.Services;

public interface IUserService
{
    Task<OneOf<AuthenticateUserResponse, ValidationFailureResponse>> AuthenticateAsync(AuthenticateUserRequest userRequest, CancellationToken cancellationToken);
    Task<UserEntity> GetAddOrUpdateAsync(UserInfo user, CancellationToken cancellationToken);
    Task GetAddOrUpdateMultipleAsync(IEnumerable<UserInfo> users, CancellationToken cancellationToken);
    Task<UserEntity?> GetAsync(string login, CancellationToken cancellationToken);
    Task<UserDto?> GetUserDtoByLoginAsync(string login, CancellationToken cancellationToken);
    Task ResetTokenAsync(string login, CancellationToken cancellationToken);
    Task<Dictionary<string, string>> GetNicknamesAsync(IEnumerable<string> logins, CancellationToken cancellationToken);
    Task<Dictionary<string, TitleUserInfo>> GetTitleUserInfosAsync(IEnumerable<string> logins, CancellationToken cancellationToken);
}

public sealed class UserService(
    AppDbContext db, 
    IZoneService zoneService, 
    ManiaPlanetIngameAPI mpIngameApi, 
    ITokenService tokenService,
    HybridCache cache,
    ILogger<UserService> logger) : IUserService
{
    public async Task<OneOf<AuthenticateUserResponse, ValidationFailureResponse>> AuthenticateAsync(AuthenticateUserRequest userRequest, CancellationToken cancellationToken)
    {
        var ingameAuthResult = await mpIngameApi.AuthenticateAsync(userRequest.User.Login, userRequest.Token, cancellationToken);

        if (!string.Equals(ingameAuthResult.Login, userRequest.User.Login, StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning("Invalid token provided for user: {UserLogin} != {AuthLogin}", userRequest.User.Login, ingameAuthResult.Login);
            return new ValidationFailureResponse("Invalid user token");
        }

        if (ingameAuthResult.Login != userRequest.User.Login)
        {
            logger.LogWarning("Case insensitive login match, weird but should be fine: {UserLogin} != {AuthLogin}", userRequest.User.Login, ingameAuthResult.Login);
        }

        userRequest.User.Login = ingameAuthResult.Login; // fix the login to lowercase in case this shit happens again

        var isAdmin = await IsAdminAsync(userRequest.User.Login, cancellationToken);

        logger.LogDebug("Generating new user token...");

        var token = tokenService.GenerateManiaPlanetUserAccessToken(userRequest.User.Login, isAdmin, out var tokenId);

        logger.LogInformation("Creating or updating user '{UserLogin}'...", userRequest.User.Login);

        var user = await GetAddOrUpdateModelAsync(userRequest.User, tokenId, interested: true, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);
        await cache.RemoveByTagAsync("user", CancellationToken.None);

        return new AuthenticateUserResponse
        {
            Login = ingameAuthResult.Login,
            Token = token,
            IsAdmin = user.IsAdmin,
        };
    }

    private async Task<UserEntity> GetAddOrUpdateModelAsync(UserInfo user, Guid? tokenId, bool interested, CancellationToken cancellationToken)
    {
        var userModel = await db.Users
            .FirstOrDefaultAsync(x => x.Id == user.Login, cancellationToken);

        if (userModel is null)
        {
            userModel = new UserEntity
            {
                Id = user.Login,
                CreatedAt = DateTimeOffset.UtcNow,
            };

            await db.Users.AddAsync(userModel, cancellationToken);
            await cache.RemoveByTagAsync("user", CancellationToken.None); // in case of error during save, the cache is still cleared, but 99.9999% it should be fine
        }

        var zoneId = await zoneService.GetZoneIdAsync(user.Zone, cancellationToken);

        // sometimes, the user doesnt send appropriate zone. make sure that its not reset
        if (zoneId.HasValue)
        {
            userModel.ZoneId = zoneId;
        }

        userModel.Nickname = user.Nickname;
        userModel.AvatarUrl = user.AvatarUrl;
        userModel.Language = user.Language;
        userModel.Description = user.Description;
        userModel.Color = user.Color;
        userModel.SteamUserId = user.SteamUserId;
        userModel.FameStars = user.FameStars;
        userModel.LadderPoints = user.LadderPoints;
        userModel.UpdatedAt = DateTimeOffset.UtcNow;

        if (tokenId.HasValue)
        {
            userModel.TokenId = tokenId.Value;
        }

        if (interested)
        {
            userModel.Interested = true;
        }

        // TODO: add last seen on server, using ClaimsPrincipal

        return userModel;
    }

    public async Task<UserEntity> GetAddOrUpdateAsync(UserInfo user, CancellationToken cancellationToken)
    {
        var userModel = await GetAddOrUpdateModelAsync(user, tokenId: null, interested: false, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);

        return userModel;
    }

    public async Task GetAddOrUpdateMultipleAsync(IEnumerable<UserInfo> users, CancellationToken cancellationToken)
    {
        foreach (var user in users)
        {
            _ = await GetAddOrUpdateModelAsync(user, tokenId: null, interested: false, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<UserEntity?> GetAsync(string login, CancellationToken cancellationToken)
    {
        return await db.Users.FindAsync([login], cancellationToken);
    }

    public async Task<UserDto?> GetUserDtoByLoginAsync(string login, CancellationToken cancellationToken)
    {
        return await db.Users
            .Where(x => x.Id == login)
            .Select(userModel => new UserDto
            {
                Login = userModel.Id,
                Nickname = userModel.Nickname,
                Zone = userModel.Zone!.Name,
                Discord = userModel.DiscordUser == null ? null : new DiscordUserDto
                {
                    Snowflake = userModel.DiscordUser.Id,
                    Username = userModel.DiscordUser.Username,
                    Nickname = userModel.DiscordUser.Nickname,
                    AvatarHash = userModel.DiscordUser.AvatarHash
                }
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task ResetTokenAsync(string login, CancellationToken cancellationToken)
    {
        var userModel = await db.Users
            .FirstOrDefaultAsync(x => x.Id == login, cancellationToken);

        if (userModel is null)
        {
            return;
        }

        userModel.TokenId = null;
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<bool> IsAdminAsync(string login, CancellationToken cancellationToken)
    {
        return await db.Users.AnyAsync(x => x.Id == login && x.IsAdmin, cancellationToken);
    }

    public async Task<Dictionary<string, string>> GetNicknamesAsync(IEnumerable<string> logins, CancellationToken cancellationToken)
    {
        return await db.Users
            .Where(x => logins.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Nickname ?? x.Id, cancellationToken);
    }

    public async Task<Dictionary<string, TitleUserInfo>> GetTitleUserInfosAsync(IEnumerable<string> logins, CancellationToken cancellationToken)
    {
        return await db.Users
            .Include(x => x.Zone)
            .Where(x => logins.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => new TitleUserInfo { Nickname = x.Nickname ?? "", Zone = x.Zone?.Name ?? "" }, cancellationToken);
    }
}
