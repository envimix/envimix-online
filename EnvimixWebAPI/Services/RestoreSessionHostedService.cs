using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace EnvimixWebAPI.Services;

public sealed class RestoreSessionHostedService(
    IServiceScopeFactory scopeFactory,
    IMemoryCache memoryCache,
    ILogger<RestoreSessionHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Attempting to restore sessions...");

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var tokens = await db.EnvimaniaSessionTokens
            .Include(x => x.Session)
                .ThenInclude(x => x.Map)
            .Where(x => x.ExpiresAt < DateTimeOffset.UtcNow)
            .ToListAsync(cancellationToken);

        if (tokens.Count == 0)
        {
            logger.LogInformation("No sessions to restore.");
            return;
        }

        logger.LogInformation("Restoring {count} sessions...", tokens.Count);

        foreach (var token in tokens)
        {
            memoryCache.Set(CacheHelper.GetEnvimaniaSessionTokenKey(token.Id), token.Session.Guid, token.ExpiresAt);
            memoryCache.Set(CacheHelper.GetEnvimaniaSessionKey(token.Session.Guid), token.Session, token.ExpiresAt);
        }

        logger.LogInformation("Sessions restored! Clearing...");

        db.EnvimaniaSessionTokens.RemoveRange(tokens);

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Cleared!");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Stopping... Current session tokens remain persist.");
        return Task.CompletedTask;
    }
}

