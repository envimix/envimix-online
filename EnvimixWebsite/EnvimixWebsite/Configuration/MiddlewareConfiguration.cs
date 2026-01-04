using EnvimixWebsite.Components;
using EnvimixWebsite.Endpoints;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

namespace EnvimixWebsite.Configuration;

public static class MiddlewareConfiguration
{
    public static void UseMiddleware(this WebApplication app)
    {
        app.UseForwardedHeaders();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseWebAssemblyDebugging();
        }
        else
        {
            app.UseExceptionHandler("/Error", createScopeForErrors: true);
            app.UseHsts();
        }

        app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
        app.UseHttpsRedirection();

        if (!app.Environment.IsDevelopment())
        {
            app.UseResponseCompression();
        }

        app.UseRateLimiter();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseOutputCache();

        app.UseAntiforgery();

        app.MapHealthChecks("/_health", new()
        {
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        }).RequireAuthorization();

        TurboEndpoints.Map(app);
        //ConnectEndpoints.Map(app);

        app.MapGet("login", async (HttpContext context, string returnUrl = "/") =>
        {
            return TypedResults.Redirect($"{app.Configuration["IdentityManager"]}/connect?returnUrl={Uri.EscapeDataString(returnUrl)}");
        });

        app.MapGet("logout", async (HttpContext context, string returnUrl = "/") =>
        {
            await context.SignOutAsync(new AuthenticationProperties() { RedirectUri = returnUrl });
        });

        app.MapStaticAssets();
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode()
            .AddInteractiveWebAssemblyRenderMode()
            .AddAdditionalAssemblies(typeof(Client._Imports).Assembly);
    }
}
