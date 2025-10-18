using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace EnvimixWebAPI.Health;

public sealed class ManiaPlanetHealthCheck(HttpClient http) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Head, "https://prod.live.maniaplanet.com");
            using var response = await http.SendAsync(request, cancellationToken);

            response.EnsureSuccessStatusCode();

            return HealthCheckResult.Healthy("ManiaPlanet is available");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("ManiaPlanet is not available", ex);
        }
    }
}
