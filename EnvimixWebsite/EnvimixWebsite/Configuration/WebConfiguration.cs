using Microsoft.AspNetCore.ResponseCompression;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using EnvimixWebsite.Authentication;

namespace EnvimixWebsite.Configuration;

public static class WebConfiguration
{
    public static void AddWebServices(this IServiceCollection services, IConfiguration config)
    {
        services.AddRazorComponents()
            .AddInteractiveServerComponents()
            .AddInteractiveWebAssemblyComponents();

        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.LoginPath = "/connect/discord";
            })
            .AddDiscord(options =>
            {
                options.ClientId = config["Discord:ClientId"] ?? throw new InvalidOperationException("Discord ClientId is missing");
                options.ClientSecret = config["Discord:ClientSecret"] ?? throw new InvalidOperationException("Discord ClientSecret is missing");
                options.ClaimActions.MapJsonKey(DiscordAdditionalClaims.GlobalName, "global_name");
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(Policies.InsiderPolicy, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireClaim(ClaimTypes.NameIdentifier, config.GetSection("Insiders").Get<string[]>() ?? []);
            });

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
