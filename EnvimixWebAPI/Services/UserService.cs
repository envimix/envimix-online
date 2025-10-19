using EnvimixWebAPI.Dtos;
using EnvimixWebAPI.Entities;
using EnvimixWebAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using OneOf;

namespace EnvimixWebAPI.Services;

public interface IUserService
{
    Task<UserEntity> GetAddOrUpdateAsync(UserInfo user, CancellationToken cancellationToken = default);
    Task GetAddOrUpdateMultipleAsync(IEnumerable<UserInfo> users, CancellationToken cancellationToken = default);
    Task<UserEntity?> GetAsync(string login, CancellationToken cancellationToken = default);
    Task<UserDto?> GetUserDtoByLoginAsync(string login, CancellationToken cancellationToken);
    Task<OneOf<UserDto, ValidationFailureResponse>> UpdateUserAsync(string login, UpdateUserRequest request, CancellationToken cancellationToken);
}

public sealed class UserService(AppDbContext db, IZoneService zoneService) : IUserService
{
    private async Task<UserEntity> GetAddOrUpdateModelAsync(UserInfo user, CancellationToken cancellationToken)
    {
        var zone = await db.Zones.FirstOrDefaultAsync(x => x.Name == user.Zone, cancellationToken);

        var userModel = await db.Users
            .Include(x => x.Zone)
            .Include(x => x.Records)
            .FirstOrDefaultAsync(x => x.Id == user.Login, cancellationToken);

        if (userModel is null)
        {
            userModel = new UserEntity
            {
                Id = user.Login
            };

            await db.Users.AddAsync(userModel, cancellationToken);
        }

        userModel.Nickname = user.Nickname;
        userModel.Zone = zone;
        userModel.AvatarUrl = user.AvatarUrl;
        userModel.Language = user.Language;
        userModel.Description = user.Description;
        userModel.Color = user.Color;
        userModel.SteamUserId = user.SteamUserId;
        userModel.FameStars = user.FameStars;
        userModel.LadderPoints = user.LadderPoints;

        // TODO: add last seen on server, using ClaimsPrincipal

        return userModel;
    }

    public async Task<UserEntity> GetAddOrUpdateAsync(UserInfo user, CancellationToken cancellationToken = default)
    {
        var userModel = await GetAddOrUpdateModelAsync(user, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);

        return userModel;
    }

    public async Task GetAddOrUpdateMultipleAsync(IEnumerable<UserInfo> users, CancellationToken cancellationToken = default)
    {
        foreach (var user in users)
        {
            _ = await GetAddOrUpdateModelAsync(user, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<UserEntity?> GetAsync(string login, CancellationToken cancellationToken = default)
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

    public async Task<OneOf<UserDto, ValidationFailureResponse>> UpdateUserAsync(string login, UpdateUserRequest request, CancellationToken cancellationToken)
    {
        var zones = await zoneService.GetZonesAsync(cancellationToken);

        if (!zones.TryGetValue(request.Zone, out var zoneId))
        {
            return new ValidationFailureResponse("Invalid Zone");
        }

        var userModel = await db.Users
            .Include(x => x.DiscordUser)
            .FirstOrDefaultAsync(x => x.Id == login, cancellationToken);

        if (userModel is null)
        {
            userModel = new UserEntity
            {
                Id = login
            };

            await db.Users.AddAsync(userModel, cancellationToken);
        }

        userModel.Nickname = request.Nickname;
        userModel.ZoneId = zoneId;

        if (request.DiscordSnowflake is not null)
        {
            var discordUser = await db.DiscordUsers
                .FirstOrDefaultAsync(x => x.Id == request.DiscordSnowflake, cancellationToken);

            userModel.DiscordUser = discordUser;
        }

        await db.SaveChangesAsync(cancellationToken);

        return new UserDto
        {
            Login = userModel.Id,
            Nickname = userModel.Nickname,
            Zone = userModel.Zone?.Name,
            Discord = userModel.DiscordUser is null ? null : new DiscordUserDto
            {
                Snowflake = userModel.DiscordUser.Id,
                Username = userModel.DiscordUser.Username,
                Nickname = userModel.DiscordUser.Nickname,
                AvatarHash = userModel.DiscordUser.AvatarHash
            }
        };
    }
}
