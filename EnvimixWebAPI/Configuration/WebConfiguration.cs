using EnvimixWebAPI.Health;
using ManiaAPI.ManiaPlanetAPI.Extensions.Hosting;
using ManiaAPI.Xml.Extensions.Hosting;

namespace EnvimixWebAPI.Configuration;

public static class WebConfiguration
{
    public static void AddWebServices(this IServiceCollection services, IConfiguration config)
    {
        services.AddHttpClient(Consts.ManiaPlanet).AddStandardResilienceHandler();
        services.AddHttpClient(Consts.ManiaPlanetWebServices).AddStandardResilienceHandler();

        services.AddManiaPlanetAPI();
        services.AddMasterServerMP4();

        services.AddAuthentication();
        services.AddAuthorization();

        services.AddOpenApi();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = 429;
        });

        services.AddHealthChecks()
            .AddDbContextCheck<AppDbContext>()
            .AddCheck<ManiaPlanetHealthCheck>(Consts.ManiaPlanet)
            .AddCheck<ManiaPlanetWebServicesHealthCheck>(Consts.ManiaPlanetWebServices);

        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
        });

        services.AddSingleton(TimeProvider.System);

    }
}
