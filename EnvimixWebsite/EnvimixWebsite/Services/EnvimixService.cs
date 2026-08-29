using Microsoft.AspNetCore.Authentication;

namespace EnvimixWebsite.Services;

public interface IEnvimixService
{
    Task RegisterServerAsync(string serverLogin, CancellationToken cancellationToken = default);
    Task<HashSet<string>> GetRegisteredServersAsync(IEnumerable<string> serverLogins, CancellationToken cancellationToken = default);
}

public sealed class EnvimixService(
    HttpClient httpClient,
    IConfiguration config,
    IHttpContextAccessor httpContextAccessor,
    ILogger<EnvimixService> logger) : IEnvimixService
{
    public async Task RegisterServerAsync(string serverLogin, CancellationToken cancellationToken = default)
    {
        var httpContext = httpContextAccessor.HttpContext;
        var accessToken = httpContext is null ? null : await httpContext.GetTokenAsync("access_token");
        if (string.IsNullOrEmpty(accessToken))
        {
            logger.LogWarning("No access token available while registering server {ServerLogin}", serverLogin);
            return;
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{config["EnvimixApi"]}/envimania/register")
        {
            Content = JsonContent.Create(new { ServerLogin = serverLogin })
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.UnprocessableEntity)
        {
            logger.LogWarning("Failed to register server {ServerLogin}. Status: {StatusCode}", serverLogin, response.StatusCode);
        }
    }

    public async Task<HashSet<string>> GetRegisteredServersAsync(
        IEnumerable<string> serverLogins,
        CancellationToken cancellationToken = default)
    {
        var query = string.Join("&", serverLogins.Select(x => $"serverLogin={Uri.EscapeDataString(x)}"));
        if (string.IsNullOrEmpty(query))
        {
            return new(StringComparer.OrdinalIgnoreCase);
        }

        var registered = await httpClient.GetFromJsonAsync<string[]>(
            $"{config["EnvimixApi"]}/envimania/registered?{query}",
            cancellationToken) ?? [];

        return new HashSet<string>(registered, StringComparer.OrdinalIgnoreCase);
    }
}
