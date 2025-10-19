using EnvimixWebAPI.Health;
using ManiaAPI.ManiaPlanetAPI;
using ManiaAPI.ManiaPlanetAPI.Extensions.Hosting;
using ManiaAPI.Xml.Extensions.Hosting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace EnvimixWebAPI.Configuration;

public static class WebConfiguration
{
    public static void AddWebServices(this IServiceCollection services, IConfiguration config)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = config["Jwt:Issuer"],
                    ValidAudiences = [Consts.EnvimaniaSession],
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!)),
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(Policies.EnvimaniaSessionPolicy, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireClaim("aud", Consts.EnvimaniaSession);
            });

        services.AddHttpClient(Consts.ManiaPlanet).AddStandardResilienceHandler();
        services.AddHttpClient(Consts.ManiaPlanetWebServices).AddStandardResilienceHandler();

        services.AddManiaPlanetAPI(options =>
        {
            options.Credentials = new ManiaPlanetAPICredentials(
                config["ManiaPlanet:ClientId"]!,
                config["ManiaPlanet:ClientSecret"]!);
        });

        services.AddMasterServerMP4();
        services.AddHttpClient<ManiaPlanetIngameAPI>()
            .ConfigureHttpClient(client =>
            {
                client.BaseAddress = new Uri(ManiaPlanetIngameAPI.BaseAddress);
            });

        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();
        });

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
