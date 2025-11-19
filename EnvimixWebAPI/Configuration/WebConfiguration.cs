using EnvimixWebAPI.Health;
using ManiaAPI.ManiaPlanetAPI;
using ManiaAPI.ManiaPlanetAPI.Extensions.Hosting;
using ManiaAPI.Xml.Extensions.Hosting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Threading.RateLimiting;

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
                    ValidAudiences = [Consts.EnvimaniaSession, Consts.ManiaPlanetUser],
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Convert.FromHexString(config["Jwt:Key"]!))
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(Policies.EnvimaniaSessionPolicy, policy =>
            {
                policy.RequireAuthenticatedUser()
                    .RequireClaim(JwtRegisteredClaimNames.Aud, Consts.EnvimaniaSession);
            })
            .AddPolicy(Policies.ManiaPlanetUserPolicy, policy =>
             {
                 policy.RequireAuthenticatedUser()
                    .RequireClaim(JwtRegisteredClaimNames.Aud, Consts.ManiaPlanetUser);
             })
            .AddPolicy(Policies.AdminPolicy, policy =>
            {
                policy.RequireAuthenticatedUser()
                    .RequireRole(Roles.Admin)
                    .RequireClaim(JwtRegisteredClaimNames.Aud, Consts.ManiaPlanetUser);
            })
            .AddPolicy(Policies.SuperAdminPolicy, policy =>
            {
                policy.RequireAuthenticatedUser()
                    .RequireRole(Roles.SuperAdmin)
                    .RequireClaim(JwtRegisteredClaimNames.Aud, Consts.ManiaPlanetUser);
            });

        services.AddManiaPlanetAPI(options =>
        {
            options.Credentials = new ManiaPlanetAPICredentials(
                config["ManiaPlanet:ClientId"]!,
                config["ManiaPlanet:ClientSecret"]!);
        }).AddStandardResilienceHandler();

        services.AddMasterServerMP4(x => x.AddStandardResilienceHandler(), x => x.AddStandardResilienceHandler());
        services.AddHttpClient<ManiaPlanetIngameAPI>()
            .ConfigureHttpClient(client =>
            {
                client.BaseAddress = new Uri(ManiaPlanetIngameAPI.BaseAddress);
            })
            .AddStandardResilienceHandler();

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
            options.AddPolicy("20PerHour", httpContext =>
            {
                var remoteIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: remoteIp,
                    partition => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 20,                   // Allow 20 requests
                        Window = TimeSpan.FromHours(1),     // Per 1 hour window
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0                      // No queuing, reject immediately
                    });
            });
        });

        services.AddHealthChecks()
            .AddDbContextCheck<AppDbContext>()
            .AddCheck<ManiaPlanetHealthCheck>("ManiaPlanet")
            .AddCheck<ManiaPlanetWebServicesHealthCheck>("ManiaPlanetWebServices");

        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
        });

        services.AddSingleton(TimeProvider.System);

    }
}
