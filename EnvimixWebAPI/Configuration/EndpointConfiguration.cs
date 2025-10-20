using EnvimixWebAPI.Endpoints;

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
                alpha = true
            });
        });

        EnvimaniaEndpoints.Map(app.MapGroup("/envimania"));
        InsiderEndpoints.Map(app.MapGroup("/insiders"));
        Endpoints.MapEndpoints.Map(app.MapGroup("/maps"));
        RateEndpoints.Map(app.MapGroup("/rate"));
        TotdEndpoints.Map(app.MapGroup("/totd"));
        ZoneEndpoints.Map(app.MapGroup("/zones"));
        UserEndpoints.Map(app.MapGroup("/users"));
        ActivityEndpoints.Map(app.MapGroup("/activity"));
    }
}
