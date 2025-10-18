using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace EnvimixWebAPI.Health;

public sealed class ManiaPlanetWebServicesHealthCheck(HttpClient http) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Head, "https://maniaplanet.com/webservices/swagger");
            using var response = await http.SendAsync(request, cancellationToken);

            response.EnsureSuccessStatusCode();

            return HealthCheckResult.Healthy("ManiaPlanet Web Services are available");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("ManiaPlanet Web Services are not available", ex);
        }
    }
}
