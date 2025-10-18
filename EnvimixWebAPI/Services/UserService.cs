using EnvimixWebAPI.Entities;
using EnvimixWebAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace EnvimixWebAPI.Services;

public interface IUserService
{
    Task<UserEntity> GetAddOrUpdateAsync(UserInfo user, CancellationToken cancellationToken = default);
    Task GetAddOrUpdateMultipleAsync(IEnumerable<UserInfo> users, CancellationToken cancellationToken = default);
    Task<UserEntity?> GetAsync(string login, CancellationToken cancellationToken = default);
}

public sealed class UserService(AppDbContext db) : IUserService
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
}
