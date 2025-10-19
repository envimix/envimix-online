using EnvimixWebAPI.Services;

namespace EnvimixWebAPI.Configuration;

public static class DomainConfiguration
{
    public static void AddDomainServices(this IServiceCollection services)
    {
        services.AddHostedService<InitiateZonesBackgroundService>();
        services.AddHostedService<RestoreSessionHostedService>();

        services.AddScoped<IEnvimaniaService, EnvimaniaService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IMapService, MapService>();
        services.AddScoped<IModService, ModService>();
        services.AddScoped<IZoneService, ZoneService>();
        services.AddScoped<IRatingService, RatingService>();
        services.AddScoped<IStarService, StarService>();
        services.AddScoped<IInsiderService, InsiderService>();
        services.AddScoped<ITotdService, TotdService>();
    }
}
