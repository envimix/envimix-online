using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;

namespace EnvimixWebAPI.Health;

public sealed class HealthCheckAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IConfiguration configuration) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "EnvimixWebsiteHealthCheck";
    public const string HeaderName = "X-Envimix-Health-Key";
    public const string ApiKeyConfigurationKey = "HealthChecks:EnvimixOnlineKey";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var expectedKey = configuration[ApiKeyConfigurationKey];
        var suppliedKey = Request.Headers[HeaderName].ToString();

        if (string.IsNullOrWhiteSpace(expectedKey)
            || string.IsNullOrWhiteSpace(suppliedKey)
            || !CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expectedKey),
                Encoding.UTF8.GetBytes(suppliedKey)))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid health-check credentials."));
        }

        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, "EnvimixWebsite")],
            SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
