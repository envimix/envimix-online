using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;
using System.Net.Http.Json;
using System.Text.Json;

namespace EnvimixWebsite.Configuration;

public static class AuthenticationConfiguration
{
    public static void AddAuthenticationServices(this IServiceCollection services, IConfiguration config, IHostEnvironment environment)
    {
        services.AddCascadingAuthenticationState();

        services.AddDataProtection().SetApplicationName("GbxTools");

        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.LoginPath = "/login";
                options.AccessDeniedPath = "/access-denied";
                options.LogoutPath = "/logout";

                options.Cookie.Name = ".GbxTools.Auth.v1";
                if (!environment.IsDevelopment())
                {
                    options.Cookie.Domain = ".gbx.tools"; // ← shared across subdomains
                }
                options.Cookie.Path = "/";
                options.Cookie.SameSite = SameSiteMode.None; // required for OAuth
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.IsEssential = true;

                options.Events.OnValidatePrincipal = async context =>
                {
                    var accessToken = context.Properties.GetTokenValue("access_token");
                    var refreshToken = context.Properties.GetTokenValue("refresh_token");
                    if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(refreshToken))
                    {
                        return;
                    }

                    try
                    {
                        if (!IsExpiring(accessToken))
                        {
                            return;
                        }

                        var httpClientFactory = context.HttpContext.RequestServices.GetRequiredService<IHttpClientFactory>();
                        var httpClient = httpClientFactory.CreateClient();
                        using var response = await httpClient.PostAsJsonAsync(
                            $"{config["IdentityManager"]}/api/tokens/refresh",
                            new RefreshTokenRequest(refreshToken),
                            context.HttpContext.RequestAborted);

                        if (!response.IsSuccessStatusCode)
                        {
                            var logger = context.HttpContext.RequestServices
                                .GetRequiredService<ILoggerFactory>()
                                .CreateLogger(nameof(AuthenticationConfiguration));
                            logger.LogWarning("Identity token refresh failed with status {StatusCode}", response.StatusCode);
                            return;
                        }

                        var tokens = await response.Content.ReadFromJsonAsync<TokenPair>(context.HttpContext.RequestAborted);
                        if (tokens is null)
                        {
                            return;
                        }

                        context.Properties.UpdateTokenValue("access_token", tokens.AccessToken);
                        context.Properties.UpdateTokenValue("refresh_token", tokens.RefreshToken);
                        context.ShouldRenew = true;
                    }
                    catch (Exception ex)
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILoggerFactory>()
                            .CreateLogger(nameof(AuthenticationConfiguration));
                        logger.LogWarning(ex, "Unable to refresh identity tokens");
                    }
                };
            });
    }

    private static bool IsExpiring(string accessToken)
    {
        try
        {
            var segments = accessToken.Split('.');
            if (segments.Length != 3)
            {
                return true;
            }

            using var payload = JsonDocument.Parse(WebEncoders.Base64UrlDecode(segments[1]));
            return !payload.RootElement.TryGetProperty("exp", out var expiration)
                || DateTimeOffset.FromUnixTimeSeconds(expiration.GetInt64()) <= DateTimeOffset.UtcNow.AddMinutes(5);
        }
        catch (Exception)
        {
            return true;
        }
    }

    private sealed record RefreshTokenRequest(string RefreshToken);
    private sealed record TokenPair(string AccessToken, string RefreshToken);
}
