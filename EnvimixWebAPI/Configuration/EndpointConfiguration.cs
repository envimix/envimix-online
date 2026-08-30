using EnvimixWebAPI.Endpoints;
using SimplestGitSourceGenerator;

namespace EnvimixWebAPI.Configuration;

public static class EndpointConfiguration
{
    public static void MapEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/", async (context) =>
        {
            await context.Response.WriteAsJsonAsync(new
            {
                message = "Welcome to Envimix Web API!",
                alpha = false,
                gitCommitHash = SimplestGit.CommitHash,
                gitCommitDate = SimplestGit.CommitDate,
                gitBranch = SimplestGit.Branch,
            });
        });

        EnvimaniaEndpoints.Map(app.MapGroup("/envimania"));
        PlayerEndpoints.Map(app.MapGroup("/players"));
        RecordEndpoints.Map(app.MapGroup("/records"));
        CarEndpoints.Map(app.MapGroup("/cars"));
        InsiderEndpoints.Map(app.MapGroup("/insiders"));
        Endpoints.MapEndpoints.Map(app.MapGroup("/maps"));
        RateEndpoints.Map(app.MapGroup("/rate"));
        TotdEndpoints.Map(app.MapGroup("/totd"));
        ZoneEndpoints.Map(app.MapGroup("/zones"));
        UserEndpoints.Map(app.MapGroup("/users"));
        TitleEndpoints.Map(app.MapGroup("/titles"));
        GhostEndpoints.Map(app.MapGroup("/ghosts"));
    }
}
