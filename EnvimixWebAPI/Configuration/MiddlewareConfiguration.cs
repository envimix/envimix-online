using HealthChecks.UI.Client;
using Scalar.AspNetCore;

namespace EnvimixWebAPI.Configuration;

public static class MiddlewareConfiguration
{
    public static void UseMiddleware(this WebApplication app)
    {
        app.UseForwardedHeaders();
        app.UseHttpsRedirection();

        if (!app.Environment.IsDevelopment())
        {
            app.UseResponseCompression();
        }

        app.UseRateLimiter();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseOutputCache();

        app.MapHealthChecks("/_health", new()
        {
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        }).RequireAuthorization();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi().CacheOutput();
            app.MapScalarApiReference(options =>
            {
                options.Theme = ScalarTheme.DeepSpace;
            });
        }

        app.MapEndpoints();
    }
}
