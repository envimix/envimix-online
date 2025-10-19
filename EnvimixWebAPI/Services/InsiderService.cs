using Microsoft.EntityFrameworkCore;

namespace EnvimixWebAPI.Services;

public interface IInsiderService
{
    Task<List<string>> GetAllUserIdsAsync(CancellationToken cancellationToken);
    Task<string?> GetByUserIdAsync(string userId, CancellationToken cancellationToken);
    Task<int> AddInsidersAsync(string[] userIds, CancellationToken cancellationToken);
}

public sealed class InsiderService(AppDbContext db) : IInsiderService
{
    public async Task<List<string>> GetAllUserIdsAsync(CancellationToken cancellationToken)
    {
        return await db.Users
            .Where(u => u.IsInsider)
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<string?> GetByUserIdAsync(string userId, CancellationToken cancellationToken)
    {
        return await db.Users
            .Where(u => u.IsInsider && u.Id == userId)
            .Select(u => u.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<int> AddInsidersAsync(string[] userIds, CancellationToken cancellationToken)
    {
        return await db.Users
            .Where(u => userIds.Contains(u.Id))
            .ExecuteUpdateAsync(u => u.SetProperty(user => user.IsInsider, true), cancellationToken);
    }
}
