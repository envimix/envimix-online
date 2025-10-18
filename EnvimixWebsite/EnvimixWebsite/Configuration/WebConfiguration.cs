using Microsoft.AspNetCore.ResponseCompression;

namespace EnvimixWebsite.Configuration;

public static class WebConfiguration
{
    public static void AddWebServices(this IServiceCollection services, IConfiguration config)
    {
        services.AddRazorComponents()
            .AddInteractiveServerComponents()
            .AddInteractiveWebAssemblyComponents();

        services.AddAuthentication();
        services.AddAuthorization();

        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();
        });

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = 429;
        });

        services.AddHealthChecks();

        services.AddSingleton(TimeProvider.System);
    }
}
