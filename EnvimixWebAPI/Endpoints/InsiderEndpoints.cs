using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnvimixWebAPI.Endpoints;

public static class InsiderEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        group.WithTags("Insider");

        group.MapGet("", Insiders)
            .RequireAuthorization(Policies.SuperAdminPolicy)
            .CacheOutput();

        group.MapGet("{id}", InsiderById).CacheOutput();

        group.MapPost("", AddInsiders).RequireAuthorization(Policies.SuperAdminPolicy);
    }

    private static async Task<IResult> Insiders(AppDbContext db, CancellationToken cancellationToken)
    {
        var insiders = await db.Users
            .Where(u => u.IsInsider)
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(insiders);
    }

    private static async Task<IResult> InsiderById(string id, AppDbContext db, CancellationToken cancellationToken)
    {
        var insider = await db.Users
            .Where(u => u.IsInsider && u.Id == id)
            .Select(u => u.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (insider is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(insider);
    }

    private static async Task<IResult> AddInsiders([FromBody] string[] insiders, AppDbContext db, CancellationToken cancellationToken)
    {
        var users = await db.Users
            .Where(u => insiders.Contains(u.Id))
            .ToListAsync(cancellationToken);

        foreach (var user in users)
        {
            user.IsInsider = true;
        }

        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok();
    }
}
