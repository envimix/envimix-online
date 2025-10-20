using Microsoft.AspNetCore.Http.HttpResults;

namespace EnvimixWebAPI.Endpoints;

public static class ActivityEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        group.WithTags("Activity");

        group.MapGet("leaderboard", GetLeaderboard);
    }

    private static async Task<Ok> GetLeaderboard(string titleId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
