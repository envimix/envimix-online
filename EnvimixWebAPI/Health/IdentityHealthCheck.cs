using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace EnvimixWebAPI.Health;

public sealed class IdentityHealthCheck(HttpClient http, IConfiguration configuration) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(configuration["IdentityManagerPublic"], UriKind.Absolute, out var identityUri))
        {
            return HealthCheckResult.Unhealthy("The GbxTools Identity address is not configured.");
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Head, identityUri);
            using var response = await http.SendAsync(request, cancellationToken);

            response.EnsureSuccessStatusCode();

            return HealthCheckResult.Healthy("GbxTools Identity is available");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("GbxTools Identity is not available", ex);
        }
    }
}
