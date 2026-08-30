using EnvimixWebsite.Components;
using EnvimixWebsite.Endpoints;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Caching.Hybrid;
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
        ServerEndpoints.Map(app);
        RecordEndpoints.Map(app);
        //ConnectEndpoints.Map(app);

        app.MapGet("login", async (HttpContext context, string returnUrl = "/") =>
        {
            var absoluteReturnUrl = GetAbsoluteReturnUrl(context.Request, returnUrl);
            return TypedResults.Redirect($"{app.Configuration["IdentityManagerPublic"] ?? app.Configuration["IdentityManager"]}/connect?returnUrl={Uri.EscapeDataString(absoluteReturnUrl)}");
        });

        app.MapGet("login/maniaplanet", async (HttpContext context, HybridCache cache, CancellationToken cancellationToken, string returnUrl = "/") =>
        {
            await cache.RemoveAsync($"identity_user_{context.User.FindFirstValue(ClaimTypes.NameIdentifier)}", cancellationToken);
            var absoluteReturnUrl = GetAbsoluteReturnUrl(context.Request, returnUrl);
            return TypedResults.Redirect($"{app.Configuration["IdentityManagerPublic"] ?? app.Configuration["IdentityManager"]}/connect/maniaplanet?returnUrl={Uri.EscapeDataString(absoluteReturnUrl)}");
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

    private static string GetAbsoluteReturnUrl(HttpRequest request, string returnUrl)
    {
        var websiteRoot = $"{request.Scheme}://{request.Host}{request.PathBase}";
        if (returnUrl.StartsWith('/'))
        {
            return $"{websiteRoot}{returnUrl}";
        }

        if (Uri.TryCreate(returnUrl, UriKind.Absolute, out var absoluteUri))
        {
            return string.Equals(absoluteUri.Host, request.Host.Host, StringComparison.OrdinalIgnoreCase)
                ? absoluteUri.ToString()
                : websiteRoot;
        }

        return websiteRoot;
    }
}
